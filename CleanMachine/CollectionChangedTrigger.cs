using log4net;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

namespace CleanMachine
{
    public class CollectionChangedTrigger : TriggerBase
    {
        private readonly int _tripCount;

        public CollectionChangedTrigger(INotifyCollectionChanged source, ILog logger)
            : this(source, -1, logger)
        {
        }

        public CollectionChangedTrigger(INotifyCollectionChanged source, int tripCount, ILog logger)
            : base($"{source.GetType().Name}.{nameof(source.CollectionChanged)}", source, logger)
        {
            _tripCount = tripCount;
        }

        public INotifyCollectionChanged Collection
        {
            get { return Source as INotifyCollectionChanged; }
        }

        public override Type SourceType => typeof(INotifyCollectionChanged);

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
            //TODO: investigate NewItems and OldItems.  what do they hold?
            if (args.NewItems != null && args.OldItems != null && args.NewItems.Count != args.OldItems.Count)
            {
                var collection = sender as ICollection;
                // Get a local copy of the count because making multiple queries in here seems to give different results.
                var itemCount = collection.Count;
                if (_tripCount < 0 || _tripCount == itemCount)
                {
                    //TODO: Log ("CollectionChangedTrigger tripping for item count change: {0} items remaining.",
                    //              Source is ICollection ? collection.Count.ToString(CultureInfo.InvariantCulture) : "unknown"));
                    Trip(sender, args);
                }
                else
                {
                    //TODO: log ("CollectionChangedTrigger ignoring change on collection with {0} items.", collection.Count));
                }
            }
        }
    }
}
