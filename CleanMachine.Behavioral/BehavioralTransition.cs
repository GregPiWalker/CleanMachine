//using System;
//using System.Text;
//using System.Linq;
//using System.Reactive.Concurrency;
//using log4net;
//using CleanMachine.Interfaces;

//namespace CleanMachine.Behavioral
//{
//    public class BehavioralTransition : Transition, ITransitionBehavior
//    {
//        private readonly IScheduler _scheduler;
//        private IBehavior _effect;

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="context">Description of this <see cref="BehavioralTransition"/>'s greater context.</param>
//        /// <param name="stereotype"></param>
//        /// <param name="fromState"></param>
//        /// <param name="toState"></param>
//        /// <param name="logger"></param>
//        /// <param name="scheduler"></param>
//        public BehavioralTransition(string context, string stereotype, BehavioralState fromState, BehavioralState toState, ILog logger, IScheduler scheduler = null)
//            : base(context, stereotype, fromState, toState, logger)
//        {
//            _scheduler = scheduler;
//        }

//        public BehavioralTransition(string context, BehavioralState fromState, BehavioralState toState, ILog logger, IScheduler scheduler = null)
//            : this(context, null, fromState, toState, logger, scheduler)
//        { }


//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="context">Description of this <see cref="BehavioralTransition"/>'s greater context.</param>
//        /// <param name="stereotype"></param>
//        /// <param name="toState"></param>
//        /// <param name="logger"></param>
//        /// <param name="scheduler"></param>
//        protected BehavioralTransition(string context, string stereotype, BehavioralState toState, ILog logger, IScheduler scheduler = null)
//            : this(context, stereotype, null, toState, logger, scheduler)
//        {
//        }

//        public override bool IsPassive => !_triggers.Any();

//        public IBehavior Effect
//        {
//            get { return _effect; }
//            set
//            {
//                if (!Editable)
//                {
//                    throw new InvalidOperationException($"Transition {Name} must be editable in order to set the effect.");
//                }

//                _effect = value;
//            }
//        }

//        public override string ToString()
//        {
//            StringBuilder sb = new StringBuilder($"\"{_context}({Name}): ");
//            for (int i = 0; i < _triggers.Count; i++)
//            {
//                sb.Append(_triggers[i].ToString());
//                if (i + 1 < _triggers.Count)
//                {
//                    sb.Append(", ");
//                }
//            }

//            if (Guard != null)
//            {
//                sb.Append(Guard.ToString());
//            }
            
//            if (Effect != null)
//            {
//                sb.Append(" / ").Append(Effect.ToString());
//            }

//            return sb.Append("\"").ToString();
//        }

//        /// <summary>
//        /// Attempt to traverse this transition.  If the attempt succeeds, the supplier state will be exited,
//        /// then the consumer state will be entered, then the Effect will be invoked or scheduled, and finally the
//        /// <see cref="Succeeded"/> event will be raised.
//        /// Scheduling the Effect keeps the flow of external behaviors synchronized.
//        /// </summary>
//        /// <param name="args">TripEventArgs related to the attempt to traverse.</param>
//        /// <returns>True if a transition attempt was made; false otherwise.  NOT an indicator for transition success.</returns>
//        internal override bool AttemptTraverse(TripEventArgs args)
//        {
//            if (!ValidateAttempt(args.SignalArgs))
//            {
//                return false;
//            }

//            if (args.SignalArgs is TriggerEventArgs)
//            {
//                _logger.Info($"({Name}).{nameof(AttemptTraverse)}: transitioning on behalf of '{(args.SignalArgs as TriggerEventArgs).Trigger}' trigger.");
//            }
//            else
//            {
//                _logger.Info($"({Name}).{nameof(AttemptTraverse)}: transitioning due to signal.");
//            }

//            From.Exit(this);
//            To.Enter(args);
//            _logger.Info($"({Name}).{nameof(AttemptTraverse)}: transition complete.");
            
//            if (Effect != null)
//            {
//                _logger.Debug($"({Name}).{nameof(AttemptTraverse)}: running EFFECT.");
//                if (_scheduler == null)
//                {
//                    Effect?.Invoke()
//                }
//                else
//                {
//                    _scheduler.Schedule(Effect);
//                }
//            }

//            OnSucceeded(args.SignalArgs);

//            return true;
//        }
//    }
//}
