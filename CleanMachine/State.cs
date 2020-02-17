using System;
using System.Collections.Generic;
using System.Linq;
using CleanMachine.Interfaces;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using System.Threading;
using System.Collections.ObjectModel;
using log4net;

namespace CleanMachine
{
    public class State : IState
    {
        protected readonly IScheduler _scheduler;
        protected readonly ILog _logger;
        private readonly List<Transition> _outboundTransitions = new List<Transition>();
        private readonly ManualResetEvent _entryCompletedSignal = new ManualResetEvent(false);
        private readonly List<Action<IState>> _doBehaviors = new List<Action<IState>>();

        private Action<ITransition> _entryBehavior;
        private Action<ITransition> _exitBehavior;
        private bool _isCurrentState;
        
        public State(string name, ILog logger, IScheduler behaviorScheduler = null)
        {
            Name = name;
            _scheduler = behaviorScheduler;
            _logger = logger;
        }

        public event EventHandler<StateEnteredEventArgs> EntryInitiated;
        public event EventHandler<StateEnteredEventArgs> EntryCompleted;
        public event EventHandler<StateExitedEventArgs> ExitInitiated;
        public event EventHandler<StateExitedEventArgs> ExitCompleted;
        public event EventHandler<Interfaces.TransitionEventArgs> TransitionSucceeded;
        public event EventHandler<Interfaces.TransitionEventArgs> TransitionFailed;

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

        internal bool IsEnabled { get; private set; }

        internal bool Editable { get; private set; }

        internal BooleanDisposable SelectionContext { get; private set; }

        public override string ToString()
        {
            return Name;
        }

        public void LogDiagnostics()
        {
            //TODO
        }

        public void AddDoBehavior(Action<IState> behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to add a DO behavior.");
            }

            _doBehaviors.Add(behavior);
        }

        internal void Edit()
        {
            Editable = true;

            foreach (var transition in _outboundTransitions)
            {
                transition.Edit();
            }

            _logger.Debug($"State {Name}:  editing enabled.");
        }

        internal void CompleteEdit()
        {
            Editable = false;

            foreach (var transition in _outboundTransitions)
            {
                transition.CompleteEdit();
            }

            _logger.Debug($"State {Name}:  editing completed.");
        }

        internal void SetEntryBehavior(Action<ITransition> behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to set the ENTRY behavior.");
            }

            _entryBehavior = behavior;
        }

        internal void SetExitBehavior(Action<ITransition> behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to set the EXIT behavior.");
            }

            _exitBehavior = behavior;
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
        internal void Enter(Transition enterOn)
        {            
            _logger.Debug($"Entering state {Name}.");

            IsCurrentState = true;
            //_entryCompletedSignal.Reset();

            OnEntryInitiated(enterOn);

            if (_entryBehavior != null)
            {
                if (_scheduler == null)
                {
                    OnEntryBehavior(enterOn);
                }
                else
                {
                    _scheduler.Schedule(enterOn, (_, t) => { return OnEntryBehavior(t); });
                }
            }

            // Schedule a signal for the entry completion.
            //_scheduler.Schedule(() => _entryCompletedSignal.Set());

            OnEntryCompleted(enterOn);

            // Now that all ENTRY work is complete, enable all transition triggers.
            Enable();

            ScheduleDoBehaviors();
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
        internal void Exit(Transition exitOn)
        {
            _logger.Debug($"Exiting state {Name}.");
            
            IsCurrentState = false;
            Disable();

            // Wait for certain completion of all entry behaviors.
            //_entryCompletedSignal.WaitOne();

            OnExitInitiated(exitOn);

            if (_exitBehavior != null)
            {
                if (_scheduler == null)
                {
                    OnExitBehavior(exitOn);
                }
                else
                {
                    _scheduler.Schedule(exitOn, (_, t) => { return OnExitBehavior(t); });
                }
            }
            
            OnExitCompleted(exitOn);
        }

        internal void SetAsInitialState()
        {
            if (IsEnabled)
            {
                return;
            }

            IsCurrentState = true;
        }

        internal Transition CreateTransitionTo(string context, State consumer)
        {
            var transition = new Transition(context, this, consumer, _logger, _scheduler);
            AddTransition(transition);
            return transition;
        }

        internal void AddTransition(Transition t)
        {
            _outboundTransitions.Add(t);
            t.Succeeded += HandleTransitionSucceeded;
            t.Failed += HandleTransitionFailed;
        }

        internal void Enable()
        {
            // Start a new state selection context in order to associate all incoming trigger handlers
            // with a single state selection.
            SelectionContext = new BooleanDisposable();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="otherStateName"></param>
        /// <returns></returns>
        //internal bool TryTransitionTo(object source, string otherStateName)
        //{
        //    var possibleTransitions = _outboundTransitions.Where(t => t.To.Name.Equals(otherStateName)).ToList();
        //    if (!possibleTransitions.Any())
        //    {
        //        return false;
        //    }

        //    var triggerArgs = new TriggerEventArgs();
        //    triggerArgs.Cause = source;
        //    foreach (var possibleTransition in possibleTransitions)
        //    {
        //        if (possibleTransition.AttemptTransition(triggerArgs))
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        protected IDisposable OnEntryBehavior(ITransition enteredOn)
        {
            try
            {
                _logger.Debug($"State {Name}:  performing ENTRY behavior.");
                _entryBehavior?.Invoke(enteredOn);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} during ENTRY behavior in state {Name}.", ex);
            }

            return Disposable.Empty;
        }

        protected void ScheduleDoBehaviors()
        {
            _logger.Debug($"State {Name}:  performing DO behaviors.");

            if (_scheduler == null)
            {
                _doBehaviors.ForEach(a => OnDoBehavior(a));
            }
            else
            {
                // Schedule all the DO behaviors independently.
                for (int i = 0; i < _doBehaviors.Count; i++)
                {
                    _scheduler.Schedule(_doBehaviors[i], (_, a) => { return OnDoBehavior(a); });
                }
            }
        }

        protected IDisposable OnExitBehavior(ITransition exitedOn)
        {
            try
            {
                _logger.Debug($"State {Name}:  performing EXIT behavior.");
                _exitBehavior?.Invoke(exitedOn);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} during EXIT behavior in state {Name}.", ex);
            }

            return Disposable.Empty;
        }

        private IDisposable OnDoBehavior(Action<IState> doBehavior)
        {
            // State changes don't need to wait for all the DO behaviors to finish.
            if (!IsCurrentState)
            {
                _logger.Debug($"State {Name}:  DO behavior ignored because {Name} is no longer the current state.");
                return Disposable.Empty;
            }
            
            try
            {
                doBehavior?.Invoke(this);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} during DO behavior in state {Name}.", ex);
            }

            return Disposable.Empty;
        }

        private void OnEntryInitiated(Transition enteredOn)
        {
            try
            {
                //TODO: trace logging

                var enteredArgs = enteredOn == null ? new StateEnteredEventArgs() { State = this } : enteredOn.ToIStateEnteredArgs(null);
                EntryInitiated?.Invoke(this, enteredArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(EntryInitiated)}' event in state {Name}.", ex);
            }
        }

        private void OnEntryCompleted(Transition enteredOn)
        {
            try
            {
                //TODO: trace logging

                var enteredArgs = enteredOn == null ? new StateEnteredEventArgs() { State = this } : enteredOn.ToIStateEnteredArgs(null);
                EntryCompleted?.Invoke(this, enteredArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(EntryCompleted)}' event in state {Name}.", ex);
            }
        }

        private void OnExitInitiated(Transition exitedOn)
        {
            try
            {
                //TODO: trace logging

                var exitArgs = exitedOn == null ? new StateExitedEventArgs() { State = this } : exitedOn.ToIStateExitedArgs(null);
                ExitInitiated?.Invoke(this, exitArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(ExitInitiated)}' event in state {Name}.", ex);
            }
        }

        private void OnExitCompleted(Transition exitedOn)
        {
            try
            {
                //TODO: trace logging
                var exitArgs = exitedOn == null ? new StateExitedEventArgs() { State = this } : exitedOn.ToIStateExitedArgs(null);
                ExitCompleted?.Invoke(this, exitArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(ExitCompleted)}' event in state {Name}.", ex);
            }
        }

        private void HandleTransitionSucceeded(object sender, Interfaces.TransitionEventArgs args)
        {
            try
            {
                TransitionSucceeded?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(TransitionSucceeded)}' event in state {Name}.", ex);
            }
        }

        private void HandleTransitionFailed(object sender, Interfaces.TransitionEventArgs args)
        {
            try
            {
                TransitionFailed?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(TransitionFailed)}' event in state {Name}.", ex);
            }
        }
    }
}
