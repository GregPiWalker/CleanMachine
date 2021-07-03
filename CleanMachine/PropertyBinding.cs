using log4net;
using System;
using System.ComponentModel;
using System.Reflection;

namespace CleanMachine
{
    /// <summary>
    /// This class creates a chain of <see cref="INotifyPropertyChanged"/> event handlers
    /// corresponding to a chain of property names in a string.
    /// If a single property value in the chain changes, that change propogates down the
    /// line of descendants.  This behavior mimics that of .NET Bindings provided by 
    /// the ComponentModel.
    /// </summary>
    public class PropertyBinding : IDisposable
    {
        private readonly Logger _logger;

        /// <summary>
        /// The <see cref="PropertyBinding"/> corresponding to the next descendant property in the chain.
        /// </summary>
        private readonly PropertyBinding _child;

        private readonly PropertyBinding _parent;

        /// <summary>
        /// 
        /// </summary>
        private INotifyPropertyChanged _propertyOwner;

        private PropertyInfo _propertyInfo;

        /// <summary>
        /// Create a chain of <see cref="PropertyBinding"/>s where this instance is a parent
        /// associated to the most precedent property name and the remainder of the property name
        /// chain is passed on to a child <see cref="PropertyBinding"/>.
        /// </summary>
        /// <param name="propertyNameChain"></param>
        /// <param name="logger"></param>
        public PropertyBinding(string propertyNameChain, PropertyBinding parent, Logger logger)
        {
            _logger = logger;
            _parent = parent;

            if (!string.IsNullOrEmpty(propertyNameChain) && propertyNameChain.Contains("."))
            {
                // Get the most precedent property name from the first component of the property name chain.
                var splitIndex = propertyNameChain.IndexOf(".", StringComparison.OrdinalIgnoreCase);
                PropertyName = propertyNameChain.Substring(0, splitIndex);
                // Strip PropertyName off the chain and give the remainder to a new child object.
                var remainder = propertyNameChain.Substring(splitIndex + 1, propertyNameChain.Length - splitIndex - 1);
                _child = new PropertyBinding(remainder, this, logger);
            }
            else
            {
                // In this case, there is no child PropertyBinding.  This guy is the end of the line.
                PropertyName = propertyNameChain;
            }
        }

        /// <summary>
        /// Raised by the terminal child property in the property chain when any one
        /// of the descendent's linked property changes.
        /// </summary>
        public event EventHandler<BoundPropertyChangedEventArgs> BoundPropertyChanged;

        public string PropertyName { get; }

        /// <summary>
        /// Gets the <see cref="PropertyBinding"/> corresponding to the first property in the chain.
        /// </summary>
        private PropertyBinding First => (_parent == null) ? this : _parent.First;

        /// <summary>
        /// Gets/sets this binding's property owner, and propagates the owner into a child.
        /// </summary>
        public INotifyPropertyChanged PropertyOwner
        {
            get { return _propertyOwner; }
            set
            {
                if (value == _propertyOwner)
                {
                    return;
                }

                SubscribeToPropertyOwner(false);
                _propertyOwner = value;
                // Only need to set this once.
                if (_propertyInfo == null)
                {
                    _propertyInfo = _propertyOwner?.GetType().GetProperty(PropertyName);
                }
                SubscribeToPropertyOwner(true);

                // Propagate here because the initial binding owner has been set.
                PropagateOwnerAssociations();
                if (_parent == null)
                {
                    // This is the first binding in the chain and the owner association has changed,
                    // so signal out that the owner's property's value has changed.
                    OnBoundPropertyChanged(PropertyOwner, PropertyName);
                }
            }
        }

        public void Dispose()
        {
            BoundPropertyChanged = null;
            PropertyOwner = null;
            if (_child != null)
            {
                _child.Dispose();
            }
        }

        /// <summary>
        /// Walk the property chain and set property owner associations on the way.
        /// </summary>
        private void PropagateOwnerAssociations()
        {
            if (_child != null)
            {
                try
                {
                    // Each parent binding sets their child's property owner until there are no more children.
                    SetChildsPropertyOwner();
                }
                catch (Exception ex)
                {
                    _logger.Error($"{ex.GetType().Name} - {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Find the child binding's property owner using this binding's property value.
        /// </summary>
        private void SetChildsPropertyOwner()
        {
            if (_propertyOwner != null)
            {
                // This will hookup the child's PropertyChanged event handler.
                //var info = _propertyOwner.GetType().GetProperty(PropertyName);
                var value = _propertyInfo.GetValue(_propertyOwner);
                if (value != null && !(value is INotifyPropertyChanged))
                {
                    throw new ArgumentException($"Binding Error: Property {_propertyInfo.Name} on object {_propertyOwner.GetType().Name} is not an INotifyPropertyChanged instance.");
                }

                _child.PropertyOwner = value as INotifyPropertyChanged;
            }
            else
            {
                // This allows the chain of bindings to be nulled out.
                _child.PropertyOwner = null;
            }
        }

        private void SubscribeToPropertyOwner(bool subscribe)
        {
            if (_propertyOwner == null)
            {
                return;
            }

            if (subscribe)
            {
                _propertyOwner.PropertyChanged += HandlePropertyChanged;
            }
            else
            {
                _propertyOwner.PropertyChanged -= HandlePropertyChanged;
            }
        }

        /// <summary>
        /// Handle a property change directly from this binding's property owner.
        /// This only propagates a notification to the last child in the chain, it does not forward the local notification.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (!args.PropertyName.Equals(PropertyName))
            {
                return;
            }

            // This binding's property value might have changed to null or a new object.  Either way, the chain is affected.
            PropagateOwnerAssociations();

            // The first binding in the chain always needs to echo a property change from any other descendant in the chain.
            First.OnBoundPropertyChanged(PropertyOwner, PropertyName);
        }

        /// <summary>
        /// Notify that this Binding's property value changed somehow.
        /// </summary>
        private void OnBoundPropertyChanged(object sourceOwner, string sourceName)
        {
            BoundPropertyChanged?.Invoke(this, new BoundPropertyChangedEventArgs(sourceName, sourceOwner));
        }
    }

    public class BoundPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public BoundPropertyChangedEventArgs(string propertyName, object propertyOwner)
            : base(propertyName)
        {
            PropertyOwner = propertyOwner;
        }

        public object PropertyOwner { get; set; }
    }
}
