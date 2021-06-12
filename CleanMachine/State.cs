using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using log4net;
using CleanMachine.Interfaces;
using System.Reactive.Disposables;
using Unity;
using Unity.Lifetime;

namespace CleanMachine
{
    public class State : IState
    {
        protected readonly ILog _logger;
        protected readonly List<Transition> _outboundTransitions = new List<Transition>();
        
        private bool _isCurrentState;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The unique name the defines this <see cref="State"/>.</param>
        /// <param name="logger"></param>
        public State(string name, IUnityContainer runtimeContainer, ILog logger)
            : this(name, null, runtimeContainer, logger)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The unique name the defines this <see cref="State"/>.</param>
        /// <param name="stereotype"></param>
        /// <param name="logger"></param>
        public State(string name, string stereotype, IUnityContainer runtimeContainer, ILog logger)
        {
            Name = name;
            Stereotype = stereotype;
            RuntimeContainer = runtimeContainer;
            _logger = logger;
            ValidateTrips = false;
        }

        public virtual event EventHandler<StateEnteredEventArgs> Entered;
        public virtual event EventHandler<StateExitedEventArgs> Exited;
        public virtual event EventHandler<TransitionEventArgs> TransitionSucceeded;
        public virtual event EventHandler<TransitionEventArgs> TransitionFailed;

        /// <summary>
        /// Mandatory event raised when this State's entry operations are complete.
        /// </summary>
        internal protected event EventHandler<TripEventArgs> EnteredInternal;

        internal event EventHandler<bool> IsCurrentValueChanged;

        public string Name { get; protected internal set; }

        public string Stereotype { get; protected internal set; }

        public ReadOnlyCollection<ITransition> Transitions
        {
            get { return _outboundTransitions.Cast<ITransition>().ToList().AsReadOnly(); }
        }

        public IEnumerable<ITransition> this[string toStateName]
        {
            get { return FindTransitions(toStateName); }
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

        /// <summary>
        /// 
        /// </summary>
        public bool ValidateTrips { get; set; }

        /// <summary>
        /// Gets the state's latest visit identifier.  The visit identifier tracks a unique visit (entry) to this
        /// state.  It can be used to distinguish different times the same state is entered.  For instance,
        /// a transition from this state to this state will result in the same current state, but will
        /// give two different visit identifiers.  This is useful for validating a signal/trigger that
        /// requests a transition.
        /// </summary>

        internal protected BooleanDisposable VisitIdentifier { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        internal protected IUnityContainer RuntimeContainer { get; }

        internal bool IsEnabled { get; private set; }

        internal protected bool Editable { get; protected set; }

        public override string ToString()
        {
            return Name;
        }

        public void LogDiagnostics()
        {
            //TODO
        }

        public virtual IEnumerable<Transition> FindTransitions(string toState = null)
        {
            if (string.IsNullOrEmpty(toState))
            {
                return _outboundTransitions;
            }

            return _outboundTransitions.Where(t => t.Consumer.Name == toState);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enterOn"></param>
        /// <returns></returns>
        public virtual bool CanEnter(Transition enterOn)
        {
            if (IsCurrentState && enterOn != null && enterOn.Supplier != enterOn.Consumer)
            {
                _logger.Debug($"Cannot enter state {Name}.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exitOn"></param>
        /// <returns></returns>
        public virtual bool CanExit(Transition exitOn)
        {
            if (!IsCurrentState)
            {
                _logger.Debug($"Cannot exit state {Name}; not the current state.");
                return false;
            }

            return true;
        }

        internal void Edit()
        {
            if (!Editable)
            {
                _logger.Debug($"State {Name}:  editing enabled.");
            }

            Editable = true;

            foreach (var transition in _outboundTransitions)
            {
                transition.Edit();
            }

        }

        internal void CompleteEdit()
        {
            if (Editable)
            {
                _logger.Debug($"State {Name}:  editing completed.");
            }

            Editable = false;

            foreach (var transition in _outboundTransitions)
            {
                transition.CompleteEdit();
            }
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
        /// <param name="tripArgs"></param>
        internal virtual void Enter(TripEventArgs tripArgs)
        {
            BeginEntry(tripArgs);
            EndEntry(tripArgs);
        }

        protected void BeginEntry(TripEventArgs tripArgs)
        {
            _logger.Debug($"Entering state {Name}.");
            var enterOn = tripArgs?.FindLastTransition() as Transition;

            IsCurrentState = true;
            RuntimeContainer.RegisterInstance(typeof(IState), StateMachineBase.EnteredStateKey, this, new ContainerControlledLifetimeManager());
            RuntimeContainer.RegisterInstance(typeof(ITransition), StateMachineBase.EnteredOnKey, enterOn, new ContainerControlledLifetimeManager());

            if (tripArgs != null)
            {
                tripArgs.Waypoints.AddLast(new Waypoint(this));
            }
        }

        protected void EndEntry(TripEventArgs tripArgs)
        {
            OnEntered(tripArgs);

            // Now that all ENTRY work is complete, enable all transition triggers.
            // This assigns the state's SelectionContext to all the outgoing transitions.
            Enable();
        }

        /// <summary>
        /// Empty base implementation.
        /// </summary>
        /// <param name="tripArgs"></param>
        internal protected virtual void Settle(TripEventArgs tripArgs)
        {
            // Empty base implementation.
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
        /// <param name="tripArgs"></param>
        internal virtual void Exit(TripEventArgs tripArgs)
        {
            BeginExit(tripArgs);
            EndExit(tripArgs);
        }

        protected void BeginExit(TripEventArgs tripArgs)
        {
            _logger.Debug($"Exiting state {Name}.");
            IsCurrentState = false;
            Disable();

            var exitOn = tripArgs?.FindLastTransition() as Transition;
            RuntimeContainer.RegisterInstance(typeof(IState), StateMachineBase.ExitedStateKey, this, new ContainerControlledLifetimeManager());
            RuntimeContainer.RegisterInstance(typeof(ITransition), StateMachineBase.ExitedOnKey, exitOn, new ContainerControlledLifetimeManager());
        }

        protected void EndExit(TripEventArgs tripArgs)
        {
            OnExited(tripArgs);
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

        /// <summary>
        /// Enable triggers on outbound connectors.
        /// </summary>
        internal virtual void Enable()
        {
            if (ValidateTrips)
            {
                VisitIdentifier = new BooleanDisposable();
            }

            _logger.Info($"State {Name}: enabling outbound connectors.");
            _outboundTransitions.ForEach(t => t.Enable(VisitIdentifier));
            IsEnabled = true;
        }

        /// <summary>
        /// Disable triggers on outbound connectors.
        /// </summary>
        internal void Disable()
        {
            // Dispose of the visit ID so that irrelevant trips can be cancelled.
            VisitIdentifier?.Dispose();
            _logger.Info($"State {Name}: disabling outbound connectors.");
            _outboundTransitions.ForEach(t => t.Disable());
            IsEnabled = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context">Description of this <see cref="State"/>'s existential context.</param>
        /// <param name="stereotype"></param>
        /// <returns></returns>
        protected Transition CreateTransition(string context, string stereotype)
        {
            var transition = new Transition(context, stereotype, this, null, _logger);
            AddTransition(transition);
            return transition;
        }

        /// <summary>
        /// Raise the mandatory EnteredInternal event, followed by the optional Entered event.
        /// </summary>
        /// <param name="tripArgs"></param>
        protected void OnEntered(TripEventArgs tripArgs)
        {
            EnteredInternal?.Invoke(this, tripArgs);

            if (Entered == null)
            {
                return;
            }

            var enteredOn = tripArgs?.FindLastTransition() as Transition;
            if (enteredOn == null)
            {
                _logger.Debug($"State {Name}: NULL transition found in {nameof(OnEntered)}.");
                return;
            }

            try
            {
                //TODO: trace logging

                var enteredArgs = enteredOn.ToIStateEnteredArgs(tripArgs);
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

        protected void OnExited(TripEventArgs tripArgs)
        {
            if (Exited == null)
            {
                return;
            }

            var exitedOn = tripArgs?.FindLastTransition() as Transition;
            if (exitedOn == null)
            {
                _logger.Debug($"State {Name}: NULL transition found in {nameof(OnExited)}.");
                return;
            }

            try
            {
                //TODO: trace logging
                var exitArgs = exitedOn.ToIStateExitedArgs(tripArgs);
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

        protected void HandleTransitionSucceeded(object sender, TransitionEventArgs args)
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

        protected void HandleTransitionFailed(object sender, TransitionEventArgs args)
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

    internal sealed class BlankDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
