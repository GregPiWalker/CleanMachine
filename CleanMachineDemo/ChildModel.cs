﻿using log4net;
using Prism.Mvvm;
using CleanMachine.Generic;
using System;
using System.Timers;

namespace CleanMachineDemo
{
    public class ChildModel : BindableBase
    {
        private readonly ILog _logger;
        private readonly Random _randomGenerator = new Random();
        private readonly string _name;

        public ChildModel(string name, object stateSyncContext, ILog logger)
        {
            _name = name;
            _logger = logger;
            CreateStateMachine(stateSyncContext);
        }

        public StateMachine<ChildState> StateMachine { get; private set; }

        public Timer RandomTimer { get; } = new Timer() { AutoReset = false };

        private void CreateStateMachine(object stateSyncContext)
        {
            try
            {
                StateMachine = new StateMachine<ChildState>($"Child{_name} StateMachine", _logger, stateSyncContext, false);

                using (var builder = ChildMachineBuilder.BuildStateMachine(this, StateMachine))
                {
                    //TODO:  add EFFECT actions
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} while creating child StateMachine.", ex);
                throw;
            }
        }

        public void RunTimer()
        {
            RandomTimer.Interval = _randomGenerator.Next(2000, 5000);
            RandomTimer.Start();
        }

        public void StopTimer()
        {
            RandomTimer.Stop();
        }
    }
}