using System;
using log4net;
//using System.Reactive.Concurrency;
using System.Collections.Generic;
//using System.Reactive.Disposables;
using System.Threading;

namespace CleanMachine.Generic
{
    //public sealed class BehavioralStateMachine<TState> : StateMachine<TState> where TState : struct
    //{
    //    private readonly IScheduler _behaviorScheduler;
    //    private readonly IScheduler _transitionScheduler;
    //    private readonly object _synchronizationContext;

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="name"></param>
    //    /// <param name="logger"></param>
    //    /// <param name="globalSyncContext"></param>
    //    /// <param name="asynchronousTransitions">Indicates whether </param>
    //    /// <param name="asynchronousBehaviors">Indicates whether behaviors (ENTRY, EXIT, DO, EFFECT) are executed on
    //    /// a different thread from the state machine transitions and events.</param>
    //    public BehavioralStateMachine(string name, ILog logger, object globalSyncContext, bool asynchronousTransitions, bool asynchronousBehaviors)
    //        : base(name, logger, false)
    //    {
    //        _synchronizationContext = globalSyncContext;

    //        if (asynchronousTransitions)
    //        {
    //            // When configured with async transitions, this machine can operate with or without a synchronization context.
    //            _transitionScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{Name} Transition Scheduler", IsBackground = true }; });
    //        }
    //        else if (globalSyncContext == null)
    //        {
    //            // When configured with synchronous transitions, this machine must have a local synchronization context.
    //            Logger.Debug($"{Name}:  was initialized without transition synchronization.  This is not supported; a default synchronization context will be used.");
    //            _synchronizationContext = new object();
    //        }

    //        // If user requested asyncronous state & transition behaviors, assign a specific thread to a scheduler for the behaviors.
    //        if (asynchronousBehaviors)
    //        {
    //            _behaviorScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{Name} Behavior Scheduler", IsBackground = true }; });

    //            if (globalSyncContext != null)
    //            {
    //                Logger.Warn($"{Name}:  inter-machine synchronization and asynchronous behaviors were both requested.  This is not recommended.  Using asynchronous behaviors could bypass aspects of machine sychronization.");
    //            }
    //        }

    //        // Now that all schedulers are constructed, it's safe to create the states.
    //        CreateStates(typeof(TState).GetEnumNames());
    //    }

    //    internal bool HasTransitionScheduler => _transitionScheduler != null;

    //    internal bool HasBehaviorScheduler => _behaviorScheduler != null;

    //    internal override void AttemptTransition(TransitionEventArgs args)
    //    {
    //        if (_transitionScheduler == null)
    //        {
    //            AttemptTransitionSafe(args);
    //        }
    //        else
    //        {
    //            // Using this Schedule signature in order to inject in custom IDisposable.
    //            _transitionScheduler.Schedule(args, (_, a) => { return AttemptTransitionSafe(a); });
    //        }
    //    }

    //    internal IDisposable AttemptTransitionSafe(TransitionEventArgs args)
    //    {
    //        if (_synchronizationContext == null)
    //        {
    //            if (!AttemptTransitionUnsafe(args))
    //            {
    //                return Disposable.Empty;
    //            }
    //        }
    //        else
    //        {
    //            // This lock regulates all transition triggers associated to the given synchronization context.
    //            // This means that only one of any number of transitions can successfully exit the current state,
    //            // whether those transitions all exist in one state machine or are distributed across a set of machines.
    //            lock (_synchronizationContext)
    //            {
    //                if (!AttemptTransitionUnsafe(args))
    //                {
    //                    return Disposable.Empty;
    //                }
    //            }
    //        }

    //        // This allows the machine to cancel the transition request down the line, if necessary.
    //        return args.TriggerArgs.TriggerContext;
    //    }

    //    internal override void JumpToState(State jumpTo)
    //    {
    //        if (jumpTo == null)
    //        {
    //            throw new ArgumentNullException("jumpTo");
    //        }

    //        if (_transitionScheduler == null)
    //        {
    //            JumpToStateSafe(jumpTo);
    //        }
    //        else
    //        {
    //            _transitionScheduler.Schedule(() => JumpToStateSafe(jumpTo));
    //        }
    //    }

    //    internal void JumpToStateSafe(State jumpTo)
    //    {
    //        if (_synchronizationContext == null)
    //        {
    //            JumpToStateUnsafe(jumpTo);
    //        }
    //        else
    //        {
    //            lock (_synchronizationContext)
    //            {
    //                JumpToStateUnsafe(jumpTo);
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="stateNames"></param>
    //    protected override void CreateStates(IEnumerable<string> stateNames)
    //    {
    //        foreach (var stateName in stateNames)
    //        {
    //            var state = new BehavioralState(stateName, Logger, _behaviorScheduler);
    //            _states.Add(state);
    //            state.Entered += OnStateEntered;
    //            state.Exited += OnStateExited;
    //        }
    //    }

    //    protected override bool AttemptTransitionUnsafe(TransitionEventArgs args)
    //    {
    //        var triggerContext = args.TriggerArgs.TriggerContext as BooleanDisposable;
    //        if (triggerContext == null || triggerContext.IsDisposed)
    //        {
    //            Logger.Debug($"{Name}.{nameof(AttemptTransitionUnsafe)}:  invalidating transition '{args.Transition.Name}' for trigger '{args.TriggerArgs.Trigger.ToString()}' due to a state change.");
    //            return false;
    //        }

    //        bool result = base.AttemptTransitionUnsafe(args);
    //        if (result)
    //        {
    //            OnPropertyChanged(nameof(CurrentState));
    //        }

    //        return result;
    //    }

    //     protected override void HandleTransitionRequest(object sender, TriggerEventArgs args)
    //    {
    //        var triggerContext = args.TriggerContext as BooleanDisposable;
    //        if (triggerContext == null || triggerContext.IsDisposed)
    //        {
    //            return;
    //        }

    //        base.HandleTransitionRequest(sender, args);
    //    }

    //    private void OnPropertyChanged(string propertyName)
    //    {
    //        try
    //        {
    //            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Error($"{ex.GetType().Name} during 'PropertyChanged({propertyName})' event from {Name} state machine.", ex);
    //        }
    //    }
    //}
}
