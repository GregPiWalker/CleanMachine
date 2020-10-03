using System;
using System.Text;
using System.Reactive.Concurrency;
using log4net;
using CleanMachine.Interfaces;

namespace CleanMachine.Behavioral
{
    public class BehavioralTransition : Transition, ITransitionBehavior
    {
        private readonly IScheduler _scheduler;
        private Action _effect;

        public BehavioralTransition(string context, BehavioralState fromState, BehavioralState toState, ILog logger, IScheduler scheduler = null)
            : base(context, fromState, toState, logger)
        {
            _scheduler = scheduler;
        }

        public Action Effect
        {
            get { return _effect; }
            set
            {
                if (!Editable)
                {
                    throw new InvalidOperationException($"Transition {Name} must be editable in order to set the effect.");
                }

                _effect = value;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"\"{_context}({Name}): ");
            for (int i = 0; i < _triggers.Count; i++)
            {
                sb.Append(_triggers[i].ToString());
                if (i + 1 < _triggers.Count)
                {
                    sb.Append(", ");
                }
            }

            if (Guard != null)
            {
                sb.Append(Guard.ToString());
            }
            
            if (Effect != null)
            {
                sb.Append(" / ").Append(Effect.ToString());
            }

            return sb.Append("\"").ToString();
        }

        /// <summary>
        /// Attempt to traverse this transition.  If the attempt succeeds, the supplier state will be exited,
        /// then the consumer state will be entered, then the Effect will be invoked or scheduled, and finally the
        /// <see cref="Succeeded"/> event will be raised.
        /// Scheduling the Effect keeps the flow of external behaviors synchronized.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal override bool AttemptTransition(TransitionEventArgs args)
        {
            if (!ValidateAttempt(args.SignalArgs))
            {
                return false;
            }

            if (args.SignalArgs is TriggerEventArgs)
            {
                _logger.Info($"({Name}).{nameof(AttemptTransition)}: transitioning on behalf of '{(args.SignalArgs as TriggerEventArgs).Trigger}' trigger.");
            }
            else
            {
                _logger.Info($"({Name}).{nameof(AttemptTransition)}: transitioning due to signal.");
            }

            From.Exit(this);
            To.Enter(args);
            _logger.Info($"({Name}).{nameof(AttemptTransition)}: transition complete.");
            
            if (Effect != null)
            {
                _logger.Debug($"({Name}).{nameof(AttemptTransition)}: running EFFECT.");
                if (_scheduler == null)
                {
                    Effect?.Invoke();
                }
                else
                {
                    _scheduler.Schedule(Effect);
                }
            }

            OnSucceeded(args.SignalArgs);

            return true;
        }
    }
}
