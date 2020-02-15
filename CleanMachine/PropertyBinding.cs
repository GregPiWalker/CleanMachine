using System;
using System.ComponentModel;

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
        /// <summary>
        /// The <see cref="PropertyBinding"/> corresponding to the next descendant property in the chain.
        /// </summary>
        private readonly PropertyBinding _child;

        /// <summary>
        /// 
        /// </summary>
        private INotifyPropertyChanged _propertyOwner;

        /// <summary>
        /// Create a chain of <see cref="PropertyBinding"/>s where this instance is a parent
        /// associated to the most precedent property name and the remainder of the property name
        /// chain is passed on to a child <see cref="PropertyBinding"/>.
        /// </summary>
        /// <param name="propertyNameChain"></param>
        public PropertyBinding(string propertyNameChain)
        {
            if (!string.IsNullOrEmpty(propertyNameChain) && propertyNameChain.Contains("."))
            {
                // Get the most precedent property name from the first component of the property name chain.
                var splitIndex = propertyNameChain.IndexOf(".", StringComparison.OrdinalIgnoreCase);
                PropertyName = propertyNameChain.Substring(0, splitIndex);
                // Strip PropertyName off the chain and give the remainder to a new child object.
                var remainder = propertyNameChain.Substring(splitIndex + 1, propertyNameChain.Length - splitIndex - 1);
                _child = new PropertyBinding(remainder);
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
        public event EventHandler<PropertyChangedEventArgs> BoundPropertyChanged;

        public string PropertyName { get; }

        /// <summary>
        /// Gets the <see cref="PropertyBinding"/> corresponding to the last property in the chain.
        /// </summary>
        public PropertyBinding Last
        {
            get { return _child == null ? this : _child.Last; }
        }

        /// <summary>
        /// 
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
                SubscribeToPropertyOwner(true);

                // Propagate here because the initial binding owner has been set.
                PropagateOwnerAssociations();
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
            if (_child == null)
            {
                // This is the last binding in the chain and an ancestor's initial owner association was set,
                // so signal out that the owner's property's value has changed.
                OnBoundPropertyChanged();
            }
            else
            {
                // Each parent binding sets their child's property owner until there are no more children.
                SetChildsPropertyOwner();
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
                var info = _propertyOwner.GetType().GetProperty(PropertyName);
                var value = info.GetValue(_propertyOwner) as INotifyPropertyChanged;
                if (value == null)
                {
                    throw new ArgumentException($"Binding Exception: {info.Name} is not an INotifyPropertyChanged instance.");
                }

                _child.PropertyOwner = value;
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

            // Do not raise the BoundPropertyChanged event here in order to prevent a possible feedback loop of property updates.
            // If this classes user needs the local property change notification, it must listen directly to that notifier rather than this binding.

            // The last binding in the chain always needs to echo a property change from any other ancestor in the chain.
            Last.OnBoundPropertyChanged();
        }

        /// <summary>
        /// Notify that this Binding's property value changed somehow.
        /// </summary>
        private void OnBoundPropertyChanged()
        {
            BoundPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
    }
}
