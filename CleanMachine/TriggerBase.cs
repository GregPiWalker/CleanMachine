using System;
using CleanMachine.Interfaces;
using log4net;
using System.Threading.Tasks;

namespace CleanMachine
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class TriggerBase : ITrigger
    {
        protected readonly ILog _logger;
        private readonly object _sync = new object();

        public TriggerBase(string name, object source, ILog logger)
        {
            _logger = logger;
            Name = name;
            Source = source;
            IsActive = false;
        }

        public event EventHandler<TriggerEventArgs> Triggered;

        public string Name { get; protected set; }
        
        public object Source { get; protected set; }
        
        public bool VerboseLogging { get; set; }

        /// <summary>
        /// Indicates whether this trigger is connected and responding to surrounding events.
        /// </summary>
        public bool IsActive { get; protected internal set; }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Acivate this trigger, making it responsive to surrounding events.
        /// </summary>
        public void Activate()
        {
            if (!IsActive)
            {
                IsActive = true;
                Enable();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
                Disable();
            }
        }

        /// <summary>
        /// Conditionally trip the trigger.  If tripped, the <see cref="Triggered"/> event is raised.
        /// If the Guard condition is false, then the trigger is not tripped.
        /// </summary>
        /// <param name="cause">The optional object that caused the trigger to trip.</param>
        /// <param name="causeEventArgs">The optional EventArgs related to the source event.</param>
        public void Trip(object cause, EventArgs causeEventArgs)
        {
            if (IsActive && CanTrigger(causeEventArgs))
            {
                OnTriggered(cause, causeEventArgs);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="causeEventArgs"></param>
        /// <returns></returns>
        public virtual bool CanTrigger(EventArgs causeEventArgs)
        {
            //Empty base.
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void Enable()
        {
            // Empty base.
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void Disable()
        {
            // Empty base.
        }

        /// <summary>
        /// Do not schedule trigger events so that the call stack is maintained.
        /// </summary>
        /// <param name="cause"></param>
        /// <param name="causeEventArgs"></param>
        private void OnTriggered(object cause, EventArgs causeEventArgs)
        {
            try
            {
                Triggered?.Invoke(this, new TriggerEventArgs() { Cause = cause, CauseArgs = causeEventArgs, Trigger = this });
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} during {nameof(Triggered)} event from {Name} trigger.", ex);
            }
        }
    }
}
