using log4net;
using System.Collections.Generic;
using System.ComponentModel;

namespace CleanMachine
{
    public class GroupPropertyChangedTrigger<TSource> : TriggerBase where TSource : INotifyPropertyChanged
    {
        private readonly string _filterProperty;
        private readonly object _handlerSync = new object();

        public GroupPropertyChangedTrigger(IEnumerable<TSource> sourceGroup, ILog logger)
            : this(sourceGroup, string.Empty, logger)
        {
        }
        
        public GroupPropertyChangedTrigger(IEnumerable<TSource> sourceGroup, string propertyFilter, ILog logger)
            : base($"{sourceGroup.GetType().Name}.(Group)PropertyChanged", sourceGroup, logger)
        {
            _filterProperty = propertyFilter;
        }
        
        public override string ToString()
        {
            return string.IsNullOrEmpty(_filterProperty) ? Name : $"{Name}[Name=={_filterProperty}]";
        }

        private List<INotifyPropertyChanged> SourceCollection { get; set; }

        private string PropertyName { get; set; }

        protected override void Enable()
        {
            SourceCollection.ForEach(s => s.PropertyChanged += HandlePropertyChanged);
        }

        protected override void Disable()
        {
            SourceCollection.ForEach(s => s.PropertyChanged -= HandlePropertyChanged);
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(PropertyName) || args.PropertyName.Equals(PropertyName))
            {
                lock (_handlerSync)
                {
                    //TODO: get the source into the eventargs somehow
                    Source = sender as INotifyPropertyChanged;
                    Trip(sender, args);
                }
            }
        }
    }
}
