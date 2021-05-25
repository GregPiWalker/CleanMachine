using log4net;
using System;
using System.Timers;
using CleanMachine.Behavioral.Generic;
using Diversions.Mvvm;
using CleanMachine.Behavioral;
using CleanMachine.Generic;

namespace CleanMachineDemo
{
    public class ChildModel : DivertingBindableBase
    {
        private readonly ILog _logger;
        private readonly Random _randomGenerator = new Random();
        private readonly string _name;

        public ChildModel(string name, object globalSyncContext, ILog logger)
        {
            _name = name;
            _logger = logger;
            CreateStateMachine(globalSyncContext);
        }

        public StateMachine<ChildState> StateMachine { get; private set; }

        public Timer RandomTimer { get; } = new Timer() { AutoReset = false };

        private void CreateStateMachine(object globalSyncContext)
        {
            try
            {
                StateMachine = StateMachineFactory.CreateTriggerAsync<ChildState>($"Child{_name} StateMachine", _logger, globalSyncContext);

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
