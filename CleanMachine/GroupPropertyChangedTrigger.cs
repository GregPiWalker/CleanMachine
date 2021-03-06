﻿using log4net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Concurrency;

namespace CleanMachine
{
    public class GroupPropertyChangedTrigger<TSource> : TriggerBase where TSource : INotifyPropertyChanged
    {
        private readonly string _filterProperty;
        private readonly object _handlerSync = new object();

        public GroupPropertyChangedTrigger(List<TSource> sourceGroup, IScheduler scheduler, Logger logger)
            : this(sourceGroup, string.Empty, scheduler, logger)
        {
        }
        
        public GroupPropertyChangedTrigger(List<TSource> sourceGroup, string propertyFilter, IScheduler scheduler, Logger logger)
            : base($"{sourceGroup.GetType().Name}.(Group)PropertyChanged", sourceGroup, scheduler, logger)
        {
            _filterProperty = propertyFilter;
        }
        
        public override string ToString()
        {
            return string.IsNullOrEmpty(_filterProperty) ? Name : $"{Name}[Name=={_filterProperty}]";
        }

        private List<INotifyPropertyChanged> SourceCollection
        {
            get { return Source as List<INotifyPropertyChanged>; }
        }

        private string FilterProperty { get; set; }

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
            if (string.IsNullOrEmpty(FilterProperty) || args.PropertyName.Equals(FilterProperty))
            {
                // Don't assume that the event is raised on the application dispatcher or a single thread.
                lock (_handlerSync)
                {
                    Trip(sender, args);
                }
            }
        }
    }
}
