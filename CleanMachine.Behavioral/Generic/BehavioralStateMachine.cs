using System;
using System.Collections.Generic;
using log4net;
using Unity;
using CleanMachine.Generic;
using System.Linq;

namespace CleanMachine.Behavioral.Generic
{
    /// <summary>
    /// For now, this class just exists to satisfy a project dependency.
    /// TODO:  Refactor StateMachine<>, and State<>, and eliminate Behavioral namespace.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public sealed class BehavioralStateMachine<TState> : StateMachine<TState> where TState : struct
    {
        public BehavioralStateMachine(string name, ILog logger) : base(name, logger)
        {
        }

        public BehavioralStateMachine(string name, ILog logger, bool createStates) : base(name, logger, createStates)
        {
        }

        public BehavioralStateMachine(string name, IUnityContainer runtimeContainer, ILog logger, bool createStates, object synchronizer) : base(name, runtimeContainer, logger, createStates, synchronizer)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateNames"></param>
        protected override void CreateStates(IEnumerable<string> stateNames)
        {
            if (!stateNames.Any(name => name.Equals(RequiredCommonStateValue)))
            {
                throw new InvalidOperationException($"StateMachine requires a state enum that contains the value {RequiredCommonStateValue}.");
            }

            foreach (var stateName in stateNames)
            {
                var state = new BehavioralState(stateName, RuntimeContainer, Logger);
                _states.Add(state);
                state.Entered += HandleStateEntered;
                state.Exited += HandleStateExited;
            }
        }
    }
}

//namespace CleanMachine.Behavioral.Generic
//{
//    public sealed class BehavioralStateMachine<TState> : StateMachine<TState> where TState : struct
//    {
//        private readonly IScheduler _behaviorScheduler;

//        /// <summary>
//        /// Construct an asynchronous
//        /// </summary>
//        /// <param name="name">The name of this <see cref="BehavioralStateMachine"/>.</param>
//        /// <param name="logger"></param>
//        /// <param name="synchronizer">An object that is used to synchronize internal operation of this machine.</param>
//        /// <param name="asynchronousTriggers">Indicates whether all <see cref="ITrigger"/>s should trip asynchronously.</param>
//        /// <param name="asynchronousBehaviors">Indicates whether behaviors (ENTRY, EXIT, DO, EFFECT) are executed on
//        /// a different thread from the state machine transitions and events.</param>
//        public BehavioralStateMachine(string name, ILog logger, IScheduler triggerScheduler, IScheduler behaviorScheduler)
//            : base(name, logger, false)
//        {
//            //if (asynchronousTriggers)
//            //{
//            //    // When configured with async transitions, this machine can operate with or without a synchronization context.
//            //    _transitionScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{Name} Transition Scheduler", IsBackground = true }; });
//            //}
//            //else if (synchronizer == null)
//            //{
//            //    // When configured with synchronous transitions, this machine must have a local synchronization context.
//            //    Logger.Debug($"{Name}:  was initialized without transition synchronization.  This is not supported; a default synchronization context will be used.");
//            //    _synchronizer = new object();
//            //}

//            _behaviorScheduler = behaviorScheduler;
//            // If user requested asyncronous state & transition behaviors, assign a specific thread to a scheduler for the behaviors.
//            //if (asynchronousBehaviors)
//            //{
//            //    _behaviorScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{Name} Behavior Scheduler", IsBackground = true }; });

//            //    if (synchronizer != null)
//            //    {
//            //        Logger.Warn($"{Name}:  inter-machine synchronization and asynchronous behaviors were both requested.  This is not recommended.  Using asynchronous behaviors could bypass aspects of machine sychronization.");
//            //    }
//            //}

//            // Now that all schedulers are constructed, it's safe to create the states.
//            CreateStates(typeof(TState).GetEnumNames());
//        }

//        /// <summary>
//        /// Construct a fully synchronous machine using the given sychronizing object.
//        /// </summary>
//        /// <param name="name"></param>
//        /// <param name="logger"></param>
//        /// <param name="synchronizer">Supply an optional synchronization object.  This is useful if you wish to synchronize multiple machines together.</param>
//        public BehavioralStateMachine(string name, ILog logger, object synchronizer)
//            : this(name, logger, null, synchronizer)
//        {
//            if (synchronizer == null)
//            {
//                // When configured with synchronous transitions, this machine must have a local synchronization context.
//                Logger.Debug($"{Name}:  was initialized for synchronous operation with neither an IScheduler or a synchronization object.  A default synchronization object will be used.");
//                _synchronizer = new object();
//            }
//            else
//            {
//                Logger.Debug($"{Name}:  was initialized for synchronous operation.");
//            }
//        }

//        /// <summary>
//        /// Construct a fully synchronous machine.
//        /// </summary>
//        /// <param name="name"></param>
//        /// <param name="logger"></param>
//        public BehavioralStateMachine(string name, ILog logger)
//            : this(name, logger, null)
//        {
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        public IUnityContainer RuntimeContainer { get; } = new UnityContainer();

//        //internal bool HasTransitionScheduler => _transitionScheduler != null;

//        //internal bool HasBehaviorScheduler => _behaviorScheduler != null;

//        //public override bool TryTransition(TState toState)
//        //{
//        //    //TODO:  MAKE THIS HANDLE A SCHEDULED TRANSITION

//        //    var transitionArgs = new TransitionEventArgs();
//        //    var transitions = _currentState.FindTransitions(toState.ToString());
//        //    foreach (var transition in transitions)
//        //    {
//        //        transitionArgs.Transition = transition;
//        //        var result = AttemptTransition(transitionArgs);
//        //        if (result.HasValue && result.Value)
//        //        {
//        //            return true;
//        //        }
//        //    }

//        //    return false;
//        //}

//        //internal override bool? AttemptTransition(TransitionEventArgs args)
//        //{
//        //    if (_transitionScheduler == null)
//        //    {
//        //        //TODO:  need to test that this equality condition works as intended.
//        //        return AttemptTransitionSafe(args) != Disposable.Empty;
//        //    }

//        //    // Using this Schedule signature in order to inject in custom IDisposable.
//        //    _transitionScheduler.Schedule(args, (_, a) => { return AttemptTransitionSafe(a); });

//        //    //TODO: find a way to return boolean instead of nothing.
//        //    return null;
//        //}

//        //internal IDisposable AttemptTransitionSafe(TransitionEventArgs args)
//        //{
//        //    if (_synchronizer == null)
//        //    {
//        //        if (!AttemptTransitionUnsafe(args))
//        //        {
//        //            return Disposable.Empty;
//        //        }
//        //    }
//        //    else
//        //    {
//        //        // This lock regulates all transition triggers associated to the given synchronization context.
//        //        // This means that only one of any number of transitions can successfully exit the current state,
//        //        // whether those transitions all exist in one state machine or are distributed across a set of machines.
//        //        lock (_synchronizer)
//        //        {
//        //            if (!AttemptTransitionUnsafe(args))
//        //            {
//        //                return Disposable.Empty;
//        //            }
//        //        }
//        //    }

//        //    // This allows the machine to cancel the transition request down the line, if necessary.
//        //    IDisposable context = null;
//        //    var triggerArgs = args.SignalArgs as TriggerEventArgs;
//        //    if (triggerArgs != null)
//        //    {
//        //        context = triggerArgs.TriggerContext;
//        //    }

//        //    return context;
//        //}

//        //internal override void JumpToState(State jumpTo)
//        //{
//        //    if (jumpTo == null)
//        //    {
//        //        throw new ArgumentNullException("jumpTo");
//        //    }

//        //    if (_transitionScheduler == null)
//        //    {
//        //        JumpToStateSafe(jumpTo);
//        //    }
//        //    else
//        //    {
//        //        _transitionScheduler.Schedule(() => JumpToStateSafe(jumpTo));
//        //    }
//        //}

//        //internal void JumpToStateSafe(State jumpTo)
//        //{
//        //    if (_synchronizer == null)
//        //    {
//        //        JumpToStateUnsafe(jumpTo);
//        //    }
//        //    else
//        //    {
//        //        lock (_synchronizer)
//        //        {
//        //            JumpToStateUnsafe(jumpTo);
//        //        }
//        //    }
//        //}

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="stateNames"></param>
//        protected override void CreateStates(IEnumerable<string> stateNames)
//        {
//            foreach (var stateName in stateNames)
//            {
//                var state = new BehavioralState(stateName, Logger, RuntimeContainer, _behaviorScheduler);
//                _states.Add(state);
//                state.Entered += HandleStateEntered;
//                state.Exited += HandleStateExited;
//            }
//        }

//        //protected override void HandleTransitionRequest(object sender, SignalEventArgs args)
//        //{
//        //    var triggerArgs = args as TriggerEventArgs;
//        //    if (triggerArgs != null)
//        //    {
//        //        var triggerContext = triggerArgs.TriggerContext as BooleanDisposable;
//        //        if (triggerContext == null || triggerContext.IsDisposed)
//        //        {
//        //            return;
//        //        }
//        //    }

//        //    base.HandleTransitionRequest(sender, args);
//        //}

//        //private void OnPropertyChanged(string propertyName)
//        //{
//        //    try
//        //    {
//        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Logger.Error($"{ex.GetType().Name} during 'PropertyChanged({propertyName})' event from {Name} state machine.", ex);
//        //    }
//        //}
//    }
//}
