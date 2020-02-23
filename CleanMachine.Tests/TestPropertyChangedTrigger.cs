using CleanMachine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;

namespace CleanMachine.Tests
{
    [TestClass]
    public class TestPropertyChangedTrigger
    {
    }

    internal class NotifyPropertyChangedImpl : INotifyPropertyChanged
    {
        private NotifyPropertyChangedImpl _nestedObject;

        public event PropertyChangedEventHandler PropertyChanged;

        public NotifyPropertyChangedImpl NestedObject
        {
            get { return _nestedObject; }
            set
            {
                _nestedObject = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NestedObject)));
            }
        }
    }
}
