using log4net;
using System;

namespace CleanMachine
{
    public class Logger
    {
        private bool _enableTrace;

        public Logger(ILog logger)
        {
            Log = logger;
        }

        public bool Enable { get; set; }

        public bool EnableTrace { get => _enableTrace && Enable; set => _enableTrace = value; }

        public ILog Log { get; internal set; }

        public void Error(object message)
        {
            if (!Enable)
            {
                return;
            }

            Log.Error(message);
        }

        public void Error(object message, Exception ex)
        {
            if (!Enable)
            {
                return;
            }

            Log.Error(message, ex);
        }

        public void Warn(object message)
        {
            if (!Enable)
            {
                return;
            }

            Log.Warn(message);
        }

        public void Warn(object message, Exception ex)
        {
            if (!Enable)
            {
                return;
            }

            Log.Warn(message, ex);
        }

        public void Info(object message)
        {
            if (!Enable)
            {
                return;
            }

            Log.Info(message);
        }

        public void Info(object message, Exception ex)
        {
            if (!Enable)
            {
                return;
            }

            Log.Info(message, ex);
        }

        public void Debug(object message)
        {
            if (!Enable)
            {
                return;
            }

            Log.Debug(message);
        }

        public void Debug(object message, Exception ex)
        {
            if (!Enable)
            {
                return;
            }

            Log.Debug(message, ex);
        }

        public void Trace(object message)
        {
            if (!EnableTrace)
            {
                return;
            }

            Log.Debug(message);
        }

        public void Trace(object message, Exception ex)
        {
            if (!EnableTrace)
            {
                return;
            }

            Log.Debug(message, ex);
        }
    }
}
