using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;

namespace CleanMachine.Tests
{
    [TestClass]
    public class TestPropertyChangedTrigger
    {
        private static readonly ILog _logger = LogManager.GetLogger("PropertyChanged Test Logger");

        [TestMethod]
        public void SetProperty_GivenSimplePropertyFilter_WhenFirstOrdeSet_RaisesTriggeredEvent()
        {
            var firstOrder = new NotifyPropertyChangedImpl(0);

            var p = new PropertyChangedTrigger(firstOrder, "Child", _logger);
            bool triggered = false;
            int triggeredBy = -1;

            p.Triggered += (sender, args) => {
                triggeredBy = ((args.CauseArgs as BoundPropertyChangedEventArgs).PropertyOwner as NotifyPropertyChangedImpl).NestLevel;
                triggered = true;
            };
            p.Activate();
            firstOrder.Child = new NotifyPropertyChangedImpl(1);

            Assert.IsTrue(triggered);
            Assert.AreEqual(0, triggeredBy);
        }

        [TestMethod]
        public void SetProperty_GivenSimplePropertyFilter_WhenFirstOrderUnset_RaisesTriggeredEvent()
        {
            var firstOrder = GetFirstOrderObject();

            var p = new PropertyChangedTrigger(firstOrder, "Child", _logger);
            bool triggered = false;
            int triggeredBy = -1;

            p.Triggered += (sender, args) => {
                triggeredBy = ((args.CauseArgs as BoundPropertyChangedEventArgs).PropertyOwner as NotifyPropertyChangedImpl).NestLevel;
                triggered = true;
            };
            p.Activate();
            firstOrder.Child = null;

            Assert.IsTrue(triggered);
            Assert.AreEqual(0, triggeredBy);
        }

        [TestMethod]
        public void SetProperty_GivenSimplePropertyFilter_WhenSecondOrderSet_IgnoresNestedPropertyChange()
        {
            var secondOrder = GetFirstOrderObject();

            var p = new PropertyChangedTrigger(secondOrder, "Child", _logger);
            bool triggered = false;

            p.Activate();
            // hook up th event after the first depth is set so that the trigger must come from next depth.
            p.Triggered += (sender, args) => { triggered = true; };
            // set the nested property
            secondOrder.Child.Child = new NotifyPropertyChangedImpl(2);

            Assert.IsFalse(triggered);
        }

        [TestMethod]
        public void SetProperty_GivenSimplePropertyFilter_WhenSecondOrderUnset_IgnoresNestedPropertyChange()
        {
            var secondOrder = GetSecondOrderObject();

            var p = new PropertyChangedTrigger(secondOrder, "Child", _logger);
            bool triggered = false;

            p.Activate();
            // hook up the event after the first depth is set so that the trigger must come from next depth.
            p.Triggered += (sender, args) => { triggered = true; };
            // unset the nested property
            secondOrder.Child.Child = null;

            Assert.IsFalse(triggered);
        }

        [TestMethod]
        public void SetProperty_GivenThirdOrderNesting_WhenFirstOrderSet_RaisesTriggeredEvent()
        {
            var secondOrder = new NotifyPropertyChangedImpl(1);
            secondOrder.Child = new NotifyPropertyChangedImpl(2);
            secondOrder.Child.Child = new NotifyPropertyChangedImpl(3);
            var thirdOrder = new NotifyPropertyChangedImpl(0);

            var p = new PropertyChangedTrigger(thirdOrder, "Child.Child.Child", _logger);
            bool triggered = false;
            int triggeredBy = -1;

            p.Triggered += (sender, args) => {
                triggeredBy = ((args.CauseArgs as BoundPropertyChangedEventArgs).PropertyOwner as NotifyPropertyChangedImpl).NestLevel;
                triggered = true;
            };
            p.Activate();
            thirdOrder.Child = secondOrder;

            Assert.IsTrue(triggered);
            Assert.AreEqual(0, triggeredBy);
        }

        [TestMethod]
        public void SetProperty_GivenSecondOrderNesting_WhenThirdOrderSet_RaisesTriggeredEvent()
        {
            var thirdOrder = GetSecondOrderObject();

            var p = new PropertyChangedTrigger(thirdOrder, "Child.Child.Child", _logger);
            bool triggered = false;
            int triggeredBy = -1;

            p.Triggered += (sender, args) => {
                triggeredBy = ((args.CauseArgs as BoundPropertyChangedEventArgs).PropertyOwner as NotifyPropertyChangedImpl).NestLevel;
                triggered = true;
            };
            p.Activate();
            thirdOrder.Child.Child = new NotifyPropertyChangedImpl(3);

            Assert.IsTrue(triggered);
            Assert.AreEqual(1, triggeredBy);
        }

        [TestMethod]
        public void SetProperty_GivenThirdOrderNesting_WhenSecondOrderUnset_RaisesTriggeredEvent()
        {
            var thirdOrder = GetThirdOrderObject();

            var p = new PropertyChangedTrigger(thirdOrder, "Child.Child.Child", _logger);
            bool triggered = false;
            int triggeredBy = -1;

            p.Triggered += (sender, args) => {
                triggeredBy = ((args.CauseArgs as BoundPropertyChangedEventArgs).PropertyOwner as NotifyPropertyChangedImpl).NestLevel;
                triggered = true;
            };
            p.Activate();
            thirdOrder.Child = null;

            Assert.IsTrue(triggered);
            Assert.AreEqual(0, triggeredBy);
        }

        [TestMethod]
        public void SetProperty_GivenThirdOrderNesting_WhenThirdOrderUnset_RaisesTriggeredEvent()
        {
            var thirdOrder = GetThirdOrderObject();

            var p = new PropertyChangedTrigger(thirdOrder, "Child.Child.Child", _logger);
            bool triggered = false;

            int triggeredBy = -1;

            p.Triggered += (sender, args) => {
                triggeredBy = ((args.CauseArgs as BoundPropertyChangedEventArgs).PropertyOwner as NotifyPropertyChangedImpl).NestLevel;
                triggered = true;
            };
            p.Activate();
            thirdOrder.Child.Child = null;

            Assert.IsTrue(triggered);
            Assert.AreEqual(1, triggeredBy);
        }

        [TestMethod]
        public void SetProperty_GivenThirdOrderNesting_WhenThirdOrderSet_RaisesTriggeredEvent()
        {
            var thirdOrder = GetThirdOrderObject();

            var p = new PropertyChangedTrigger(thirdOrder, "Child.Child.Child", _logger);
            bool triggered = false;
            int triggeredBy = -1;

            p.Triggered += (sender, args) => {
                triggeredBy = ((args.CauseArgs as BoundPropertyChangedEventArgs).PropertyOwner as NotifyPropertyChangedImpl).NestLevel;
                triggered = true;
            };
            p.Activate();
            thirdOrder.Child.Child.Child = null;
            
            Assert.IsTrue(triggered);
            Assert.AreEqual(2, triggeredBy);
        }

        private NotifyPropertyChangedImpl GetFirstOrderObject()
        {
            var firstOrder = new NotifyPropertyChangedImpl(0);
            firstOrder.Child = new NotifyPropertyChangedImpl(1);

            return firstOrder;
        }

        private NotifyPropertyChangedImpl GetSecondOrderObject()
        {
            var secondOrder = new NotifyPropertyChangedImpl(0);
            secondOrder.Child = new NotifyPropertyChangedImpl(1);
            secondOrder.Child.Child = new NotifyPropertyChangedImpl(2);

            return secondOrder;
        }

        private NotifyPropertyChangedImpl GetThirdOrderObject()
        {
            var thirdOrder = new NotifyPropertyChangedImpl(0);
            thirdOrder.Child = new NotifyPropertyChangedImpl(1);
            thirdOrder.Child.Child = new NotifyPropertyChangedImpl(2);
            thirdOrder.Child.Child.Child = new NotifyPropertyChangedImpl(3);

            return thirdOrder;
        }
    }
    
    internal class NotifyPropertyChangedImpl : INotifyPropertyChanged
    {
        private NotifyPropertyChangedImpl _nestedObject;

        internal NotifyPropertyChangedImpl(int nestLevel)
        {
            NestLevel = nestLevel;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NotifyPropertyChangedImpl Child
        {
            get { return _nestedObject; }
            set
            {
                _nestedObject = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Child)));
            }
        }

        public int NestLevel { get; private set; }
    }
}
