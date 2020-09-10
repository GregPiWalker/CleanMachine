using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using log4net;
using CleanMachine.Interfaces;

namespace CleanMachine
{
    public class State : IState
    {
        protected readonly ILog _logger;
        protected readonly List<Transition> _outboundTransitions = new List<Transition>();
        
        private bool _isCurrentState;
        
        public State(string name, ILog logger)
        {
            Name = name;
            _logger = logger;
        }
        
        public virtual event EventHandler<StateEnteredEventArgs> Entered;
        public virtual event EventHandler<StateExitedEventArgs> Exited;
        public virtual event EventHandler<Interfaces.TransitionEventArgs> TransitionSucceeded;
        public virtual event EventHandler<Interfaces.TransitionEventArgs> TransitionFailed;

        internal event EventHandler<bool> IsCurrentValueChanged;

        public string Name { get; }

        public ReadOnlyCollection<ITransition> Transitions
        {
            get { return _outboundTransitions.Cast<ITransition>().ToList().AsReadOnly(); }
        }

        public bool IsCurrentState
        {
            get { return _isCurrentState; }
            protected set
            {
                //TODO: debug logging
                _isCurrentState = value;
                IsCurrentValueChanged?.Invoke(this, value);
            }
        }

        //TODO: figure out a way to make this internal and internal protected
        public IDisposable SelectionContext { get; protected set; }

        internal bool IsEnabled { get; private set; }

        internal bool Editable { get; private set; }

        public override string ToString()
        {
            return Name;
        }

        public void LogDiagnostics()
        {
            //TODO
        }

        public IEnumerable<Transition> FindTransitions(string toState)
        {
            return _outboundTransitions.Where(t => t.Consumer.Name == toState);
        }

        internal void Edit()
        {
            if (Editable)
            {
                return;
            }

            Editable = true;

            foreach (var transition in _outboundTransitions)
            {
                transition.Edit();
            }

            _logger.Debug($"State {Name}:  editing enabled.");
        }

        internal void CompleteEdit()
        {
            if (!Editable)
            {
                return;
            }

            Editable = false;

            foreach (var transition in _outboundTransitions)
            {
                transition.CompleteEdit();
            }

            _logger.Debug($"State {Name}:  editing completed.");
        }

        internal bool CanEnter(Transition enterOn)
        {
            if (IsCurrentState && enterOn != null && enterOn.Supplier != enterOn.Consumer)
            {
                _logger.Debug($"Cannot enter state {Name}.");
                return false;
            }

            return true;
        }

        internal bool CanExit(Transition enterOn)
        {
            if (!IsCurrentState)
            {
                _logger.Debug($"Cannot exit state {Name}; not the current state.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Entering a state involves in order:
        /// 1) Raising <see cref="EntryInitiated"/> event.
        /// 2) Performing ENTRY behavior.
        /// 3) Raising <see cref="EntryCompleted"/> event.
        /// 4) Enabling all outgoing transitions.
        /// 5) Performing DO behaviors.
        /// 
        /// <see cref="EntryInitiated"/> and <see cref="EntryCompleted"/> are both
        /// raised before transition triggers are enabled to decrease likelihood of
        /// recursive eventing.
        /// </summary>
        /// <param name="enterOn"></param>
        internal virtual void Enter(Transition enterOn)
        {            
            _logger.Debug($"Entering state {Name}.");

            IsCurrentState = true;

            OnEntered(enterOn);

            // Now that all ENTRY work is complete, enable all transition triggers.
            Enable();
        }

        /// <summary>
        /// Exiting a state involves in order:
        /// 1) Disabling all outgoing transitions. 
        /// 2) Raising <see cref="ExitInitiated"/> event.
        /// 3) Performing EXIT behavior.
        /// 4) Raising <see cref="ExitCompleted"/> event.
        /// 
        /// <see cref="ExitInitiated"/> and <see cref="ExitCompleted"/> are both
        /// raised after transition triggers are disabled to decrease likelihood of
        /// recursive eventing.
        /// </summary>
        /// <param name="exitOn"></param>
        internal virtual void Exit(Transition exitOn)
        {
            _logger.Debug($"Exiting state {Name}.");
            
            IsCurrentState = false;
            Disable();
            
            OnExited(exitOn);
        }

        internal void SetAsInitialState()
        {
            if (IsEnabled)
            {
                return;
            }

            IsCurrentState = true;
        }

        internal virtual Transition CreateTransitionTo(string context, State consumer)
        {
            var transition = new Transition(context, this, consumer, _logger);
            AddTransition(transition);
            return transition;
        }

        internal void AddTransition(Transition t)
        {
            _outboundTransitions.Add(t);
            t.Succeeded += HandleTransitionSucceeded;
            t.Failed += HandleTransitionFailed;
        }

        internal virtual void Enable()
        {
            _logger.Info($"State {Name}: enabling all transitions.");
            _outboundTransitions.ForEach(t => t.Enable(SelectionContext));
            IsEnabled = true;
        }

        internal void Disable()
        {
            // Dispose of the selection context so that trigger handlers can be cancelled.
            SelectionContext?.Dispose();
            _logger.Info($"State {Name}: disabling all transitions.");
            _outboundTransitions.ForEach(t => t.Disable());
            IsEnabled = false;
        }

        protected void OnEntered(Transition enteredOn)
        {
            try
            {
                //TODO: trace logging

                var enteredArgs = enteredOn == null ? new StateEnteredEventArgs() { State = this } : enteredOn.ToIStateEnteredArgs(null);
                RaiseEntered(enteredArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(Entered)}' event in state {Name}.", ex);
            }
        }

        protected virtual void RaiseEntered(StateEnteredEventArgs args)
        {
            Entered?.Invoke(this, args);
        }

        protected void OnExited(Transition exitedOn)
        {
            try
            {
                //TODO: trace logging
                var exitArgs = exitedOn == null ? new StateExitedEventArgs() { State = this } : exitedOn.ToIStateExitedArgs(null);
                RaiseExited(exitArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(Exited)}' event in state {Name}.", ex);
            }
        }

        protected virtual void RaiseExited(StateExitedEventArgs args)
        {
            Exited?.Invoke(this, args);
        }

        protected void HandleTransitionSucceeded(object sender, Interfaces.TransitionEventArgs args)
        {
            try
            {
                RaiseTransitionSucceeded(args);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(TransitionSucceeded)}' event in state {Name}.", ex);
            }
        }

        protected void HandleTransitionFailed(object sender, Interfaces.TransitionEventArgs args)
        {
            try
            {
                RaiseTransitionFailed(args);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(TransitionFailed)}' event in state {Name}.", ex);
            }
        }

        protected virtual void RaiseTransitionSucceeded(Interfaces.TransitionEventArgs args)
        {
            TransitionSucceeded?.Invoke(this, args);
        }

        protected virtual void RaiseTransitionFailed(Interfaces.TransitionEventArgs args)
        {
            TransitionFailed?.Invoke(this, args);
        }
    }
}
