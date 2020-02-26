using System;
using System.Text;
using System.Reactive.Concurrency;
using log4net;
using CleanMachine.Interfaces;

namespace CleanMachine
{
    public class BehavioralTransition : Transition, ITransitionBehavior
    {
        private Action _effect;
        private readonly IScheduler _scheduler;

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
                    throw new InvalidOperationException();
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
        /// Scheduling the Effect and events keeps the flow of external behaviors synchronized.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal override bool AttemptTransition(TriggerEventArgs args)
        {
            if (!ValidateAttempt(args))
            {
                return false;
            }

            _logger.Info($"{Name}.{nameof(AttemptTransition)}: transitioning on behalf of '{args.Trigger.ToString()}' trigger.");
            From.Exit(this);
            To.Enter(this);
            _logger.Info($"{Name}.{nameof(AttemptTransition)}: transition complete.");
            
            if (Effect != null)
            {
                _logger.Debug($"{Name}.{nameof(AttemptTransition)}: running EFFECT.");
                if (_scheduler == null)
                {
                    Effect?.Invoke();
                }
                else
                {
                    _scheduler.Schedule(Effect);
                }
            }

            OnSucceeded(args);

            return true;
        }
    }
}
