using System;
using System.Collections.Generic;
using CleanMachine.Interfaces;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using log4net;

namespace CleanMachine
{
    public class BehavioralState : State, IStateBehavior
    {
        protected readonly IScheduler _scheduler;
        private readonly List<Action<IState>> _doBehaviors = new List<Action<IState>>();

        private Action<ITransition> _entryBehavior;
        private Action<ITransition> _exitBehavior;
        
        public BehavioralState(string name, ILog logger, IScheduler behaviorScheduler)
            : base(name, logger)
        {
            _scheduler = behaviorScheduler;
        }

        public event EventHandler<StateEnteredEventArgs> EntryInitiated;
        public event EventHandler<StateExitedEventArgs> ExitInitiated;

        public override string ToString()
        {
            return Name;
        }

        public void SetEntryBehavior(Action<ITransition> behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to set the ENTRY behavior.");
            }

            _entryBehavior = behavior;
        }

        public void AddDoBehavior(Action<IState> behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to add a DO behavior.");
            }

            _doBehaviors.Add(behavior);
        }

        public void SetExitBehavior(Action<ITransition> behavior)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"State {Name} must be editable in order to set the EXIT behavior.");
            }

            _exitBehavior = behavior;
        }

        internal override Transition CreateTransitionTo(string context, State consumer)
        {
            var transition = new BehavioralTransition(context, this, consumer as BehavioralState, _logger);
            AddTransition(transition);
            return transition;
        }

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
        /// <param name="enterOn"></param>
        internal override void Enter(Transition enterOn)
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

            OnEntered(enterOn);

            // Now that all ENTRY work is complete, enable all transition triggers.
            Enable();

            ScheduleDoBehaviors();
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
            
            OnExited(exitOn);
        }

        ///// <summary>
        ///// In case the scheduler was not able to be set in construction.
        ///// </summary>
        ///// <param name="behaviorScheduler"></param>
        //internal void SetScheduler(IScheduler behaviorScheduler)
        //{
        //    if (behaviorScheduler == null || _scheduler != null)
        //    {
        //        return;
        //    }

        //    _scheduler = behaviorScheduler;
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
