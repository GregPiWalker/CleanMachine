using System;
using System.Linq;
using System.Collections.Generic;
using log4net;
using CleanMachine;
using CleanMachine.Interfaces;
using CleanMachine.Behavioral.Behaviors;
using Unity;
using Unity.Lifetime;
using System.Reactive.Concurrency;

namespace CleanMachine.Behavioral
{
    public class BehavioralState : State, IStateBehavior
    {
        protected const string EntryBehaviorName = "ENTER Behavior";
        protected const string ExitBehaviorName = "EXIT Behavior";
        protected const string DoBehaviorName = "DO Behavior";
        private readonly List<IBehavior> _doBehaviors = new List<IBehavior>();

        private IBehavior _entryBehavior;
        private IBehavior _exitBehavior;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The unique name the defines this <see cref="BehavioralState"/>.</param>
        /// <param name="container"></param>
        /// <param name="logger"></param>
        public BehavioralState(string name, IUnityContainer container, ILog logger)
            : base(name, container, logger)
        {
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
            SetEntryBehavior(CreateBehavior(EntryBehaviorName, action));
        }

        public void AddDoBehavior(IBehavior behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to add a DO behavior.");
            }

            _doBehaviors.Add(behavior);
        }

        public void AddDoBehavior(string name, Action<IUnityContainer> action)
        {
            AddDoBehavior(CreateBehavior(name, action));
        }

        public void AddDoBehavior(Action<IUnityContainer> action)
        {
            var name = $"{DoBehaviorName} {_doBehaviors.Count + 1}";
            AddDoBehavior(name, action);
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
            SetExitBehavior(CreateBehavior(ExitBehaviorName, action));
        }

        private IBehavior CreateBehavior(string name, Action<IUnityContainer> action)
        {
            IBehavior behavior;
            if (RuntimeContainer.HasTypeRegistration<IScheduler>(StateMachineBase.BehaviorSchedulerKey))
            {
                behavior = new ScheduledBehavior(name, action, RuntimeContainer.Resolve<IScheduler>(StateMachineBase.BehaviorSchedulerKey));
            }
            else
            {
                behavior = new Behavior(name, action);
            }

            return behavior;
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
            BeginEntry(tripArgs);
            
            OnEntryInitiated(tripArgs);

            if (_entryBehavior != null)
            {
                OnEntryBehavior();
            }

            EndEntry(tripArgs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tripArgs"></param>
        internal protected override void Settle(TripEventArgs tripArgs)
        {
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
        internal override void Exit(TripEventArgs tripArgs)
        {
            BeginExit(tripArgs);

            OnExitInitiated(tripArgs);

            if (_exitBehavior != null)
            {
                OnExitBehavior();
            }

            EndExit(tripArgs);
        }

        protected void OnEntryBehavior()
        {
            try
            {
                _logger.Debug($"State {Name}:  performing ENTRY behavior.");
                _entryBehavior?.Invoke(RuntimeContainer);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} during ENTRY behavior in state {Name}.", ex);
            }
        }

        protected void OnExitBehavior()
        {
            try
            {
                _logger.Debug($"State {Name}:  performing EXIT behavior.");
                _exitBehavior?.Invoke(RuntimeContainer);
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
                doBehavior?.Invoke(RuntimeContainer);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} during DO behavior in state {Name}.", ex);
            }
        }

        private void OnEntryInitiated(TripEventArgs tripArgs)
        {
            if (EntryInitiated == null)
            {
                return;
            }

            var enteredOn = tripArgs?.FindLastTransition() as Transition;
            if (enteredOn == null)
            {
                _logger.Debug($"State {Name}: NULL transition found in {nameof(OnEntryInitiated)}.");
                return;
            }

            try
            {
                //TODO: trace logging

                var enteredArgs = enteredOn.ToIStateEnteredArgs(tripArgs);
                EntryInitiated?.Invoke(this, enteredArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(EntryInitiated)}' event in state {Name}.", ex);
            }
        }

        private void OnExitInitiated(TripEventArgs tripArgs)
        {
            if (ExitInitiated == null)
            {
                return;
            }

            var exitedOn = tripArgs?.FindLastTransition() as Transition;
            if (exitedOn == null)
            {
                _logger.Debug($"State {Name}: NULL transition found in {nameof(OnExitInitiated)}.");
                return;
            }

            try
            {
                //TODO: trace logging

                var exitArgs = exitedOn.ToIStateExitedArgs(tripArgs);
                ExitInitiated?.Invoke(this, exitArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} resulted from raising '{nameof(ExitInitiated)}' event in state {Name}.", ex);
            }
        }
    }
}
