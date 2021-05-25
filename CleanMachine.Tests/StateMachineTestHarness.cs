using CleanMachine;
using CleanMachine.Behavioral;
using CleanMachine.Behavioral.Behaviors;
using CleanMachine.Generic;
using CleanMachine.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using Unity;

namespace CleanMachine.Tests
{
    public class StateMachineTestHarness<TState> where TState : struct
    {
        private readonly ManualResetEvent _transitionDone = new ManualResetEvent(false);
        private readonly ManualResetEvent _transitionPreparedForDo = new ManualResetEvent(false);
        private readonly ManualResetEvent _doBehaviorDone = new ManualResetEvent(false);
        private readonly ManualResetEvent _testIsPrepared = new ManualResetEvent(false);
        private readonly Stopwatch _executionTimer = new Stopwatch();

        public StateMachineTestHarness(StateMachine<TState> machine, string initialState)
        {
            Machine = machine;
            Machine.Edit();
            Machine.SetInitialState(initialState);
            
            foreach (var state in Machine.States)
            {
                //state.EntryInitiated += State_EntryInitiated;
                //state.EntryCompleted += State_EntryCompleted;
                //state.ExitInitiated += State_ExitInitiated;
                //state.ExitCompleted += State_ExitCompleted;
                //state.TransitionSucceeded += State_TransitionSucceeded;
                //state.TransitionFailed += State_TransitionFailed;
            }
        }

        private event EventHandler<EventArgs> TestTrigger;

        public TimeSpan ExecutionTime { get { return _executionTimer.Elapsed; } }
        public List<Action> ConditionalResponses { get; set; }
        public Action SuccessAction { get; set; }
        public Action FailureAction { get; set; }

        private StateMachine<TState> Machine { get; set; }

        public void BuildOneWayMachine()
        {
            for (int i = 0; i < Machine.States.Count; i++)
            {
                if (i + 1 < Machine.States.Count)
                {
                    var trigger = new Trigger<StateMachineTestHarness<TState>, EventArgs>(this, nameof(TestTrigger), Machine.TriggerScheduler, Machine.Logger);
                    var transition = Machine.CreateTransition(Machine.States[i].Name, Machine.States[i + 1].Name);
                    transition.Edit();
                    transition.AddTrigger(trigger);

                    //transition.Succeeded += Transition_Succeeded;
                    //transition.Failed += Transition_Failed;
                }
            }
        }

        public void BuildCircularMachine()
        {
            BuildOneWayMachine();

            // Transition from the last state back to the first.
            int last = Machine.States.Count - 1;
            var trigger = new Trigger<StateMachineTestHarness<TState>, EventArgs>(this, nameof(TestTrigger), Machine.TriggerScheduler, Machine.Logger);
            var transition = Machine.CreateTransition(Machine.States[last].Name, Machine.States[0].Name);
            transition.Edit();
            transition.AddTrigger(trigger);

            //transition.Succeeded += Transition_Succeeded;
            //transition.Failed += Transition_Failed;
        }

        public void AddDoBehavior(Action<IUnityContainer> action)
        {
            var states = Machine.States.OfType<BehavioralState>().ToList();
            foreach (BehavioralState state in states)
            {
                state.AddDoBehavior(action);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        public bool WaitUntilAsyncTransitionSuccess(TimeSpan waitTime)
        {
            if (waitTime < TimeSpan.FromMilliseconds(500))
            {
                Assert.Fail("Configured waitTime value must be 500ms or greater.");
            }

            Machine.CompleteEdit();

            // Hookup transitions after initial state already entered to avoid measuring the wrong transaction.
            foreach (var state in Machine.States)
            {
                foreach (Transition transition in state.Transitions)
                {
                    transition.Succeeded += (a, b) =>
                    {
                        // Block until the outer test thread is ready.
                        if (!_testIsPrepared.WaitOne(TimeSpan.FromMilliseconds(500)))
                        {
                            return;
                        }

                        _transitionDone.Set();
                    };
                }
            }

            // In case machine is async, crudely wait for it to enter initial state .
            while (((StateMachineBase)Machine).CurrentState == null)
            {
                Thread.Sleep(50);
            }

            TripTriggerInSyncWithTransition();

            // Now wait until the transition EventHandler sets the waithandle.
            // Transition is either asynchronous or synchronous with respect to the current thread,
            // so we set up a wait point here.
            return _transitionDone.WaitOne(waitTime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        public bool WaitUntilAsyncDoBehavior(TimeSpan waitTime)
        {
            Assert.IsNotNull(Machine, "Use a BehavioralStateMachine to test asynchronous behaviors");

            if (waitTime < TimeSpan.FromMilliseconds(500))
            {
                Assert.Fail("Configured waitTime value must be 500ms or greater.");
            }

            if (Machine.HasTriggerScheduler)
            {
                return WaitUntilFullyAsyncDoBehavior(waitTime);
            }
            else
            {
                return WaitUntilPartialAsyncDoBehavior(waitTime);
            }
        }

        /// <summary>
        /// This makes the test thread wait on the transition thread which then waits on the behavior thread.
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        private bool WaitUntilFullyAsyncDoBehavior(TimeSpan waitTime)
        {
            AddDoBehavior((a) =>
            {
                // Wait until the waithandle is in the reset state before signaling done.
                if (_transitionPreparedForDo.WaitOne(TimeSpan.FromMilliseconds(1000)))
                {
                    _doBehaviorDone.Set();
                }
            });

            // Completing state machine editing will cause the DO behavior of the initial state.
            // Signal that the state is free to act by faking a prepared transition.
            _transitionPreparedForDo.Set();
            Machine.CompleteEdit();

            // Now wait to make sure the initial DO behavior set its signal so that we can reset it for the real test.
            bool doIsDone = _doBehaviorDone.WaitOne(waitTime);

            // Got initial state change out of the way, so reset everything for the test.
            _doBehaviorDone.Reset();
            _transitionPreparedForDo.Reset();

            // Hookup transitions after initial state already entered to avoid measuring the wrong transaction.
            foreach (var state in Machine.States)
            {
                foreach (Transition transition in state.Transitions)
                {
                    transition.Succeeded += (a, b) =>
                    {
                        // Block until the outer test thread is ready.
                        if (!_testIsPrepared.WaitOne(TimeSpan.FromMilliseconds(1000)))
                        {
                            return;
                        }


                        if (!SignalDoAndWaitForItToFinish(TimeSpan.FromMilliseconds(1000)))
                        {
                            return;
                        }

                        _transitionDone.Set();
                    };
                }
            }

            // In case machine is async, crudely wait for it to enter initial state .
            while (((StateMachineBase)Machine).CurrentState == null)
            {
                Thread.Sleep(50);
            }

            TripTriggerInSyncWithTransition();

            // Now wait until the transition EventHandler sets the waithandle.
            // Transition is either asynchronous or synchronous with respect to the current thread,
            // so we set up a wait point here.
            return _transitionDone.WaitOne(waitTime);
        }

        /// <summary>
        /// This makes the test thread wait directly on the behavior thread.
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        private bool WaitUntilPartialAsyncDoBehavior(TimeSpan waitTime)
        {
            AddDoBehavior((a) =>
            {
                // Wait until the waithandle is in the reset state before signaling done.
                if (_testIsPrepared.WaitOne(TimeSpan.FromMilliseconds(1000)))
                {
                    _doBehaviorDone.Set();
                }
            });

            // Completing state machine editing will cause the DO behavior of the initial state.
            // Signal that the state is free to act by faking a prepared transition.
            _testIsPrepared.Set();
            Machine.CompleteEdit();

            // Now wait to make sure the initial DO behavior set its signal so that we can reset it for the real test.
            _doBehaviorDone.WaitOne(waitTime);

            // Got initial state change out of the way, so reset everything for the test.
            _doBehaviorDone.Reset();
            _testIsPrepared.Reset();
            
            // In case machine is async, crudely wait for it to enter initial state .
            while (((StateMachineBase)Machine).CurrentState == null)
            {
                Thread.Sleep(50);
            }

            TripTriggerInSyncWithTransition();

            // Now wait until the transition EventHandler sets the waithandle.
            // Transition is either asynchronous or synchronous with respect to the current thread,
            // so we set up a wait point here.
            return _doBehaviorDone.WaitOne(waitTime);
        }

        private bool SignalDoAndWaitForItToFinish(TimeSpan waitTime)
        {
            // First set the waithandle to unblock the DO action.
            _transitionPreparedForDo.Set();

            // Then wait until the DO action finishes its work.
            return _doBehaviorDone.WaitOne(waitTime);
        }

        /// <summary>
        /// 
        /// </summary>
        private void TripTriggerInSyncWithTransition()
        {
            // First trip the trigger.
            TestTrigger(this, new EventArgs());

            // Now this will unblock the transition EventHandler so that it can set the waithandle.
            _testIsPrepared.Set();
        }

        private void Transition_Failed(object sender, TransitionEventArgs e)
        {
        }

        private void Transition_Succeeded(object sender, TransitionEventArgs e)
        {
        }

        private void State_TransitionFailed(object sender, TransitionEventArgs e)
        {
        }

        private void State_TransitionSucceeded(object sender, TransitionEventArgs e)
        {
        }

        private void State_ExitCompleted(object sender, StateExitedEventArgs e)
        {
        }

        private void State_ExitInitiated(object sender, StateExitedEventArgs e)
        {
        }

        private void State_EntryCompleted(object sender, StateEnteredEventArgs e)
        {
        }

        private void State_EntryInitiated(object sender, StateEnteredEventArgs e)
        {
        }
    }
}
