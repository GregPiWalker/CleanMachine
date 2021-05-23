using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using CleanMachine;
using CleanMachine.Behavioral.Generic;
using CleanMachine.Interfaces;
using Diversions;
using Diversions.Mvvm;
using Diversions.ObjectModel;
using CleanMachine.Behavioral;

namespace CleanMachineDemo
{
    [Diversion(MarshalOption.CurrentThread)]
    public class DemoModel : DivertingBindableBase
    {
        private readonly object _synchronizationContext = new object();
        private readonly ILog _logger;
        private bool _onOff;
        private int _number;
        private int _collectionCount;
        private int _loopCount;
        private Func<bool> _boolFunc;
        private bool _areChildrenReady;

        public DemoModel(ILog logger)
        {
            _logger = logger;
            LoopCount = 3;
            BoolFunc = () => false;
            for (int i = 0; i < 5; i++)
            {
                var child = new ChildModel((i + 1).ToString(), _synchronizationContext, logger);
                Children.Add(child);
                child.StateMachine.StateChanged += HandleChildStateChanged;
            }

            CreateStateMachine();
        }

        public event EventHandler<DemoEventArgs> TriggerEvent;

        public BehavioralStateMachine<DemoState> StateMachine { get; private set; }

        public bool OnOff
        {
            get { return _onOff; }
            set
            {
                if (SetProperty(ref _onOff, value, nameof(OnOff)))
                {
                    if (value)
                    {
                        StartChildren();
                    }
                    else
                    {
                        StopChildren();
                    }
                }
            }
        }

        public int Number
        {
            get { return _number; }
            set { SetProperty(ref _number, value, nameof(Number)); }
        }

        public int CollectionCount
        {
            get { return _collectionCount; }
            set
            {
                if (SetProperty(ref _collectionCount, value, nameof(CollectionCount)))
                {
                    UpdateCollection();
                }
            }
        }

        public int LoopCount
        {
            get { return _loopCount; }
            set { SetProperty(ref _loopCount, value, nameof(LoopCount)); }
        }

        public Func<bool> BoolFunc
        {
            get { return _boolFunc; }
            set { SetProperty(ref _boolFunc, value, nameof(BoolFunc)); }
        }

        public bool AreChildrenReady
        {
            get { return _areChildrenReady; }
            set { SetProperty(ref _areChildrenReady, value, nameof(AreChildrenReady)); }
        }

        public DivertingObservableCollection<object> Observables { get; } = new DivertingObservableCollection<object>();

        public List<ChildModel> Children { get; } = new List<ChildModel>();

        public void TriggerAll()
        {
            _logger.Debug($"{nameof(DemoModel)}: raising '{nameof(TriggerEvent)}' event.");
            TriggerEvent?.Invoke(this, new DemoEventArgs());
        }

        private void CreateStateMachine()
        {
            try
            {
                StateMachine = CleanMachine.Behavioral.StateMachineFactory.CreatePartialAsync<DemoState>("Demo StateMachine", _logger);
                foreach (var state in StateMachine.States)
                {
                    state.Exited += HandleStateExited;
                    //state.Entered += HandleStateEntered;
                }

                using (var builder = DemoMachineBuilder.BuildStateMachine(this, StateMachine))
                {
                    // If we loop around the state machine, reset the loop count variable;
                    (StateMachine[DemoState.One] as IStateBehavior).AddDoBehavior(c => { Reset(); });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} while creating demo StateMachine.", ex);
                throw;
            }
        }

        private void Reset()
        {
            LoopCount = 3;
            BoolFunc = null;
            CollectionCount = 0;
            OnOff = false;
            Number = 0;
        }

        private void StartChildren()
        {
            Children.ForEach(c => c.RunTimer());
        }

        private void StopChildren()
        {
            Children.ForEach(c => c.StopTimer());
        }

        private void UpdateCollection()
        {
            if (Observables.Count == _collectionCount)
            {
                return;
            }

            while (Observables.Count < _collectionCount)
            {
                Observables.Add(new object());
            }

            while (Observables.Count > _collectionCount)
            {
                Observables.RemoveAt(0);
            }
        }

        /// <summary>
        /// The State ExitCompleted event occurs while transitions are disabled, so modifying the
        /// <see cref="LoopCount"/> property will not create a recursive event loop.
        /// Using the StateMachine StateChanged event instead would cause a recursive loop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleStateExited(object sender, StateExitedEventArgs args)
        {
            if (args.TransitionArgs.Transition.Consumer == args.TransitionArgs.Transition.Supplier)
            {
                if (LoopCount > 0)
                {
                    LoopCount--;
                }
            }
        }

        private void HandleChildStateChanged(object sender, StateChangedEventArgs<ChildState> args)
        {
            AreChildrenReady = Children.All(c => c.StateMachine.CurrentState == ChildState.Ready);
        }
    }
}
