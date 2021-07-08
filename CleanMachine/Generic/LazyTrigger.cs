using log4net;
using System;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Linq.Expressions;
using System.ComponentModel;

namespace CleanMachine.Generic
{
    //TODO: Consider making this IDisposable

    /// <summary>
    /// Creates a trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
    /// </summary>
    /// <typeparam name="TEventSource"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    public class LazyTrigger<TEventSource, TEventArgs> : Trigger<INotifyPropertyChanged, TEventArgs> where TEventSource : class
    {
        private delegate void EventHandlerDelegate(object sender, TEventArgs args);
        private readonly object _updateSync = new object();
        private Func<TEventSource> _eventSourceProvider;
        private string _filterProperty;
        private PropertyBinding _propertyBinding;

        private bool _isEnabled;
        private TEventSource _eventSource;

        /// <summary>
        /// Creates a lazily-bound trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
        /// The lazy source will be harvested when this Trigger is both enabled and has an event source reference.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propertyNameChain"></param>
        /// <param name="eventName"></param>
        /// <param name="scheduler"></param>
        /// <param name="logger"></param>
        public LazyTrigger(INotifyPropertyChanged source, string propertyNameChain, string eventName, IScheduler scheduler, Logger logger)
            : base(source, eventName, scheduler, logger)
        {
            if (string.IsNullOrEmpty(propertyNameChain))
            {
                throw new ArgumentException("propertyNameChain");
            }

            // Hold on to the entire string.  The PropertyBindings will parse it out.
            _filterProperty = propertyNameChain;

            // Build the entire chain of PropertyBinding objects for the given nested properties.
            _propertyBinding = new PropertyBinding(propertyNameChain, null, _logger);
            // Now set the entire chain of references using the source object as the top-level parent.
            // NestedObserver's property is set now, ok to use that name.
            _propertyBinding.PropertyOwner = Source;

            // This may not have a value yet, but just in case.
            UpdateEventSource();
        }

        //public LazyTrigger(INotifyPropertyChanged source, Expression<Func<TEventSource>> propertyChainExpr, string eventName, IScheduler scheduler, Logger logger)
        //    : base(source, eventName, scheduler, logger)
        //{
        //    //TODO:
        //    ParseExpression(propertyChainExpr);
        //}

        protected override Type EventSourceType => typeof(TEventSource);

        public TEventSource EventSource
        {
            get => _eventSource;
            private set 
            {
                if (value == _eventSource)
                {
                    return;
                }

                // Always unsubscribe now if the value is changing.
                SubscribeToEventSource(false);
                _eventSource = value;
            }
        }

        public new INotifyPropertyChanged Source => base.Source as INotifyPropertyChanged;

        public override string ToString()
        {
            return Name;
        }

        protected override void Enable()
        {
            lock (_updateSync)
            {
                _isEnabled = true;
                _propertyBinding.BoundPropertyChanged += HandleNestedPropertyChanged;

                // try to subscribe to the event now, but it may not be available yet.
                UpdateEventSource();
                SubscribeToEventSource(true);
            }
        }

        protected override void Disable()
        {
            lock (_updateSync)
            {
                _isEnabled = false;
                _propertyBinding.BoundPropertyChanged -= HandleNestedPropertyChanged;

                // try to unsubscribe from the event now, but it may not be available anymore.
                SubscribeToEventSource(false);
            }
        }

        //private void ParseExpression(Expression<Func<TEventSource>> lazySource)
        //{
        //    _eventSourceProvider = lazySource.Compile();
        //    var bodyExpr = lazySource.Body as MemberExpression;
        //    var sourceExpr = bodyExpr;
        //    ConstantExpression rootExpr = null;
        //    while (sourceExpr != null && rootExpr == null)
        //    {
        //        if (sourceExpr.Expression is ConstantExpression)
        //        {
        //            rootExpr = sourceExpr.Expression as ConstantExpression;
        //        }
        //        else
        //        {
        //            sourceExpr = sourceExpr.Expression as MemberExpression;
        //        }
        //    }

        //    var rootStr = rootExpr?.ToString() ?? sourceExpr.ToString();
        //    string bodystr = lazySource.Body.ToString();
        //    var propertyNameChain = bodystr.Remove(bodystr.IndexOf(rootStr), rootStr.Length).TrimStart('.');

        //    //var source = sourceExpr.C
        //}

        /// <summary>
        /// A PropertyChanged handler that is only hooked up if this does listen to a nested property.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleNestedPropertyChanged(object sender, BoundPropertyChangedEventArgs args)
        {
            lock (_updateSync)
            {
                if (UpdateEventSource() && _isEnabled)
                {
                    // Only subscribe to a new value now if the trigger is enabled.
                    SubscribeToEventSource(true);

                    //if (IsAttachedToSourceEvent)
                    //{                        
                    //    //TODO: consider whether this should Trip() now since it's potentially a late binding
                    //    //Trip(EventSource, )
                    //}
                }
            }
        }

        private bool UpdateEventSource()
        {
            TEventSource oldSource = EventSource;
            if (_propertyBinding.Last.PropertyOwner == null)
            {
                EventSource = null;
            }
            else
            {
                object value = _propertyBinding.Last.GetPropertyValue();
                EventSource = (value == null) ? null : (TEventSource)value;
            }

            return EventSource != oldSource;
        }

        private void SubscribeToEventSource(bool subscribe)
        {
            if (_eventSource == null)
            {
                return;
            }

            if (subscribe)
            {
                AttachToSourceEvent(_eventSource);
            }
            else
            {
                DetachFromSourceEvent(_eventSource);
            }
        }
    }
}
