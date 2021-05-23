using log4net;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reactive.Concurrency;

namespace CleanMachine
{
    public class CollectionChangedTrigger : TriggerBase
    {
        private readonly int _tripFilter;

        public CollectionChangedTrigger(INotifyCollectionChanged source, IScheduler tripScheduler, ILog logger)
            : this(source, -1, tripScheduler, logger)
        {
        }

        public CollectionChangedTrigger(INotifyCollectionChanged source, int tripFilter, IScheduler tripScheduler, ILog logger)
            : base($"{source.GetType().Name}.{nameof(source.CollectionChanged)}", source, tripScheduler, logger)
        {
            _tripFilter = tripFilter;
        }

        public INotifyCollectionChanged Collection
        {
            get { return Source as INotifyCollectionChanged; }
        }

        protected override void Enable()
        {
            Collection.CollectionChanged += HandleCollectionChanged;
        }

        protected override void Disable()
        {
            Collection.CollectionChanged -= HandleCollectionChanged;
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.NewItems != null || args.OldItems != null)
            {
                var collection = sender as ICollection;
                // Get a local copy of the count because making multiple queries in here seems to give different results.
                var itemCount = collection.Count;
                if (_tripFilter < 0 || _tripFilter == itemCount)
                {
                    if (VerboseLogging)
                    {
                        _logger.Debug($"{nameof(CollectionChangedTrigger)} '{Name}' tripping for item count change: {itemCount} items remaining");
                    }
                    Trip(sender, args);
                }
                else if (VerboseLogging)
                {
                    _logger.Debug($"{nameof(CollectionChangedTrigger)} '{Name}' ignoring change on collection with {itemCount} items.");
                }
            }
        }
    }
}
