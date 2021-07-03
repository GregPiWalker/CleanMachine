using log4net;
using System.ComponentModel;
using System.Reactive.Concurrency;

namespace CleanMachine
{
    /// <summary>
    /// A trigger for the PropertyChanged event of an INotifyPropertyChanged implementor.
    /// This trigger acts like a data binding in that it can respond to a chain of hierarchal 
    /// PropertyChanged events, as long as each object in the chain implements INotifyPropertyChanged.
    /// </summary>
    public class PropertyChangedTrigger : TriggerBase
    {
        private readonly string _filterProperty;
        private readonly PropertyBinding _propertyBinding;

        /// <summary>
        /// Create an unfiltered trigger that trips for all PropertyChanged events from the source.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="tripScheduler"></param>
        /// <param name="logger"></param>
        public PropertyChangedTrigger(INotifyPropertyChanged source, IScheduler tripScheduler, Logger logger)
            : this(source, null, tripScheduler, logger)
        {
        }
        
        /// <summary>
        /// Create a trigger that responds to PropertyChanged events on INotifyPropertyChanged implementations.
        /// Providing a value in propertyNameChain causes the trigger to only trip on a property name match.
        /// As long as every property in a chain references an INotifyPropertyChanged, the name may specify
        /// a nested property, such as "Object1.Object2.Object3.Property1"
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propertyNameChain">optional property name chain to use as a filter</param>
        /// <param name="tripScheduler"></param>
        /// <param name="logger"></param>
        public PropertyChangedTrigger(INotifyPropertyChanged source, string propertyNameChain, IScheduler tripScheduler, Logger logger)
            : base($"{source.GetType().Name}.{nameof(source.PropertyChanged)}", source, tripScheduler, logger)
        {
            if (!string.IsNullOrEmpty(propertyNameChain) && propertyNameChain.Contains("."))
            {
                // Build the entire chain of PropertyBinding objects for the given nested properties.
                _propertyBinding = new PropertyBinding(propertyNameChain, null, logger);
                // Now set the entire chain of references using the source object as the top-level parent.
                //NestedObserver's property is set now, ok to use that name.
                _propertyBinding.PropertyOwner = source;
            }

            // Either way, hold on to the entire string.  For nested properties, this is needed for deep copying.
            _filterProperty = propertyNameChain;
        }

        private INotifyPropertyChanged PropertyOwner => Source as INotifyPropertyChanged;

        public override string ToString()
        {
            return string.IsNullOrEmpty(_filterProperty) ? $"{Name}[Property: *]" : $"{Name}[Property: {_filterProperty}]";
        }

        protected override void Enable()
        {
            if (_propertyBinding == null)
            {
                PropertyOwner.PropertyChanged += HandleSourcePropertyChanged;
            }
            else
            {
                _propertyBinding.BoundPropertyChanged += HandleNestedPropertyChanged;
            }
        }

        protected override void Disable()
        {
            if (_propertyBinding == null)
            {
                PropertyOwner.PropertyChanged -= HandleSourcePropertyChanged;
            }
            else
            {
                _propertyBinding.BoundPropertyChanged -= HandleNestedPropertyChanged;
            }
        }

        /// <summary>
        /// A PropertyChanged handler that is only hooked up if this does listen to a nested property.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleNestedPropertyChanged(object sender, BoundPropertyChangedEventArgs args)
        {
            Trip(sender, args);
        }

        /// <summary>
        /// A PropertyChanged handler that is only hooked up if this does not listen to a nested property.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleSourcePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(_filterProperty) || args.PropertyName.Equals(_filterProperty))
            {
                Trip(sender, new BoundPropertyChangedEventArgs(args.PropertyName, sender));
            }
        }
    }
}
