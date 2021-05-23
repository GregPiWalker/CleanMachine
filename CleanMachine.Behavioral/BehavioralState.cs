using System;
using System.Linq;
using System.Collections.Generic;
using log4net;
using CleanMachine;
using CleanMachine.Interfaces;
using CleanMachine.Behavioral.Behaviors;
using Unity;
using Unity.Lifetime;

namespace CleanMachine.Behavioral
{
    public class BehavioralState : State, IStateBehavior
    {
        protected const string EntryBehaviorName = "ENTER Behavior";
        protected const string ExitBehaviorName = "EXIT Behavior";
        protected const string DoBehaviorName = "DO Behavior";
        protected readonly IUnityContainer _runtimeContainer;
        private readonly List<IBehavior> _doBehaviors = new List<IBehavior>();

        private IBehavior _entryBehavior;
        private IBehavior _exitBehavior;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The unique name the defines this <see cref="BehavioralState"/>.</param>
        /// <param name="logger"></param>
        /// <param name="container"></param>
        /// <param name="behaviorScheduler"></param>
        public BehavioralState(string name, ILog logger, IUnityContainer container)
            : base(name, logger)
        {
            _runtimeContainer = container;
            ValidateTrips = true;
        }

        public event EventHandler<StateEnteredEventArgs> EntryInitiated;
        public event EventHandler<StateExitedEventArgs> ExitInitiated;

        public IEnumerable<Transition> PassiveTransitions => _outboundTransitions.OfType<Transition>().Where(t => t.IsPassive);

        public override string ToString()
        {
            return Name;
        }

        public void SetEntryBehavior(IBehavior behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to set the ENTRY behavior.");
            }

            _entryBehavior = behavior;
        }

        public void SetEntryBehavior(Action<IUnityContainer> action)
        {
            SetEntryBehavior(new Behavior(EntryBehaviorName, action));
        }

        public void AddDoBehavior(IBehavior behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to add a DO behavior.");
            }

            _doBehaviors.Add(behavior);
        }

        public void AddDoBehavior(Action<IUnityContainer> action)
        {
            var name = $"{DoBehaviorName} {_doBehaviors.Count + 1}";
            AddDoBehavior(new Behavior(name, action));
        }

        public void SetExitBehavior(IBehavior behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to set the EXIT behavior.");
            }

            _exitBehavior = behavior;
        }

        public void SetExitBehavior(Action<IUnityContainer> action)
        {
            SetExitBehavior(new Behavior(ExitBehaviorName, action));
        }

        //internal override Transition CreateTransitionTo(string context, State consumer)
        //{
        //    var transition = new Transition(context, this, consumer, _logger);
        //    AddTransition(transition);
        //    return transition;
        //}

        /// <summary>
        /// Entering a state involves in order:
        /// 1) Raising <see cref="EntryInitiated"/> event.
        /// 2) Performing ENTRY behavior.
        /// 3) Raising <see cref="Entered"/> event.
        /// 4) Enabling all outgoing transitions.
        /// 5) Performing DO behaviors.
        /// 
        /// <see cref="EntryInitiated"/> and <see cref="Entered"/> are both
        /// raised before transition triggers are enabled to decrease likelihood of
        /// recursive eventing.
        /// </summary>
        /// <param name="tripArgs"></param>
        internal override void Enter(TripEventArgs tripArgs)
        {
            _logger.Debug($"Entering state {Name}.");
            var enterOn = tripArgs?.FindLastTransition() as Transition;

            IsCurrentState = true;

            OnEntryInitiated(enterOn);

            if (_entryBehavior != null)
            {
                OnEntryBehavior(enterOn);
            }

            OnEntered(enterOn);

            // Now that all ENTRY work is complete, enable all transition triggers.
            Enable();

            if (_doBehaviors.Any())
            {
                _logger.Debug($"State {Name}:  performing DO behaviors.");
                _doBehaviors.ForEach(b => OnDoBehavior(b));
            }
        }

        /// <summary>
        /// Exiting a state involves in order:
        /// 1) Disabling all outgoing transitions. 
        /// 2) Raising <see cref="ExitInitiated"/> event.
        /// 3) Performing EXIT behavior.
        /// 4) Raising <see cref="Exited"/> event.
        /// 
        /// <see cref="ExitInitiated"/> and <see cref="Exited"/> are both
        /// raised after transition triggers are disabled to decrease likelihood of
        /// recursive eventing.
        /// </summary>
        /// <param name="exitOn"></param>
        internal override void Exit(Transition exitOn)
        {
            _logger.Debug($"Exiting state {Name}.");
            
            IsCurrentState = false;
            Disable();

            OnExitInitiated(exitOn);

            if (_exitBehavior != null)
            {
                OnExitBehavior(exitOn);
            }
            
            OnExited(exitOn);
        }

        protected void OnEntryBehavior(ITransition enteredOn)
        {
            try
            {
                _logger.Debug($"State {Name}:  performing ENTRY behavior.");
                _runtimeContainer.RegisterInstance(enteredOn, new ContainerControlledLifetimeManager());
                _entryBehavior?.Invoke(_runtimeContainer);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} during ENTRY behavior in state {Name}.", ex);
            }
        }

        protected void OnExitBehavior(ITransition exitedOn)
        {
            try
            {
                _logger.Debug($"State {Name}:  performing EXIT behavior.");
                _runtimeContainer.RegisterInstance(exitedOn, new ContainerControlledLifetimeManager());
                _exitBehavior?.Invoke(_runtimeContainer);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} during EXIT behavior in state {Name}.", ex);
            }
        }

        private void OnDoBehavior(IBehavior doBehavior)
        {
            // State changes don't need to wait for all the DO behaviors to finish.
            if (!IsCurrentState)
            {
                _logger.Debug($"State {Name}:  DO behavior ignored because {Name} is no longer the current state.");
                return;
            }
            
            try
            {
                _runtimeContainer.RegisterInstance(this, new ContainerControlledLifetimeManager());
                doBehavior?.Invoke(_runtimeContainer);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} during DO behavior in state {Name}.", ex);
            }
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
    }
}
