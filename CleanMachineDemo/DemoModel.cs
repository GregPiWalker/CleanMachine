﻿using log4net;
using Prism.Mvvm;
using CleanMachine.Generic;
using CleanMachine.Interfaces;
using System;
using System.Collections.ObjectModel;

namespace CleanMachineDemo
{
    public class DemoModel : BindableBase
    {
        private readonly ObservableCollection<object> _observables;
        private readonly ILog _logger;
        private bool _onOff;
        private int _number;
        private int _collectionCount;
        private int _loopCount;
        private Func<bool> _boolFunc;

        public DemoModel(ILog logger)
        {
            _logger = logger;
            _observables = new ObservableCollection<object>();
            LoopCount = 3;
            BoolFunc = () => false;
            CreateStateMachine();
        }

        public event EventHandler<DemoEventArgs> TriggerEvent;

        public StateMachine<DemoState> StateMachine { get; private set; }

        public bool OnOff
        {
            get { return _onOff; }
            set { SetProperty(ref _onOff, value, nameof(OnOff)); }
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

        public ObservableCollection<object> Observables => _observables;
        
        public void TriggerAll()
        {
            _logger.Debug($"{nameof(DemoModel)}: raising '{nameof(TriggerEvent)}' event.");
            TriggerEvent?.Invoke(this, new DemoEventArgs());
        }

        private void CreateStateMachine()
        {
            StateMachine = new StateMachine<DemoState>("DemoStateMachine", _logger);
            foreach (var state in StateMachine.States)
            {
                state.ExitCompleted += HandleStateExited;
                //state.EntryInitiated += HandleStateEntered;
            }
            
            using (var builder = DemoBuilder.BuildStateMachine(this, StateMachine))
            {
                // If we loop around the state machine, reset the loop count variable;
                StateMachine[DemoState.One].AddDoBehavior(s => { Reset(); });
            }
        }

        private void Reset()
        {
            LoopCount = 3;
            BoolFunc = null;
            CollectionCount = 0;
        }

        private void UpdateCollection()
        {
            if (_observables.Count == _collectionCount)
            {
                return;
            }

            while (_observables.Count < _collectionCount)
            {
                _observables.Add(new object());
            }

            while (_observables.Count > _collectionCount)
            {
                _observables.RemoveAt(0);
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
    }
}
