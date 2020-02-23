using CleanMachine;
using CleanMachine.Generic;
using CleanMachine.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CleanMachine.Tests
{
    public class StateMachineHarness
    {
        private const int StartTimeoutMs = 1000;
        private const int FinishTimeoutMs = 60000;
        private readonly ManualResetEvent _transitionDone = new ManualResetEvent(false);
        private readonly ManualResetEvent _transitionPreparedForDo = new ManualResetEvent(false);
        private readonly ManualResetEvent _behaviorThreadEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent _testPreparedForTransition = new ManualResetEvent(false);
        private readonly Stopwatch _executionTimer = new Stopwatch();

        public StateMachineHarness(StateMachine machine, string initialState)
        {
            Machine = machine;
            Machine.Edit();
            Machine.SetInitialState(initialState);
            
            foreach (State state in Machine.States)
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

        private StateMachine Machine { get; set; }

        public void BuildOneWayMachine()
        {
            var states = Machine.States.Cast<State>().ToList();
            for (int i = 0; i < states.Count; i++)
            {
                if (i + 1 < states.Count)
                {
                    var trigger = new Trigger<StateMachineHarness, EventArgs>(this, nameof(TestTrigger), Machine.Logger);
                    var transition = Machine.CreateTransition(states[i].Name, states[i + 1].Name);
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
            var states = Machine.States.Cast<State>().ToList();
            int last = states.Count - 1;
            var trigger = new Trigger<StateMachineHarness, EventArgs>(this, nameof(TestTrigger), Machine.Logger);
            var transition = Machine.CreateTransition(states[last].Name, states[0].Name);
            transition.Edit();
            transition.AddTrigger(trigger);

            //transition.Succeeded += Transition_Succeeded;
            //transition.Failed += Transition_Failed;
        }

        public void AddDoBehavior(Action<IState> action)
        {
            foreach (State state in Machine.States)
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
            foreach (State state in Machine.States)
            {
                foreach (Transition transition in state.Transitions)
                {
                    transition.Succeeded += (a, b) =>
                    {
                        // Block until the outer test thread is ready.
                        if (!_testPreparedForTransition.WaitOne(TimeSpan.FromMilliseconds(500)))
                        {
                            return;
                        }

                        _transitionDone.Set();
                    };
                }
            }

            // In case machine is async, crudely wait for it to enter initial state .
            while (Machine.CurrentState == null)
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
            if (waitTime < TimeSpan.FromMilliseconds(500))
            {
                Assert.Fail("Configured waitTime value must be 500ms or greater.");
            }

            AddDoBehavior((a) =>
            {
                // Wait until the waithandle is in the reset state before signaling done.
                if (_transitionPreparedForDo.WaitOne(TimeSpan.FromMilliseconds(500)))
                {
                    _behaviorThreadEvent.Set();
                }
            });

            // Completing state machine editing will cause the DO behavior of the initial state.
            // Signal that the state is free to act by faking a prepared transition.
            _transitionPreparedForDo.Set();
            Machine.CompleteEdit();

            // Now wait to make sure the initial DO behavior set its signal so that we can reset it for the real test.
            _behaviorThreadEvent.WaitOne(waitTime);

            // Got initial state change out of the way, so reset everything for the test.
            _behaviorThreadEvent.Reset();
            _transitionPreparedForDo.Reset();

            // Hookup transitions after initial state already entered to avoid measuring the wrong transaction.
            foreach (State state in Machine.States)
            {
                foreach (Transition transition in state.Transitions)
                {
                    transition.Succeeded += (a, b) =>
                    {
                        // Block until the outer test thread is ready.
                        if (!_testPreparedForTransition.WaitOne(TimeSpan.FromMilliseconds(500)))
                        {
                            return;
                        }


                        if (!BlockUntilDoBehaviorCompletes(waitTime))
                        {
                            return;
                        }

                        _transitionDone.Set();
                    };
                }
            }

            // In case machine is async, crudely wait for it to enter initial state .
            while (Machine.CurrentState == null)
            {
                Thread.Sleep(50);
            }

            TripTriggerInSyncWithTransition();

            // Now wait until the transition EventHandler sets the waithandle.
            // Transition is either asynchronous or synchronous with respect to the current thread,
            // so we set up a wait point here.
            return _transitionDone.WaitOne(waitTime);
        }

        private bool BlockUntilDoBehaviorCompletes(TimeSpan waitTime)
        {
            // First set the waithandle to unblock the DO action.
            _transitionPreparedForDo.Set();

            // Then wait until the DO action finishes its work.
            return _behaviorThreadEvent.WaitOne(TimeSpan.FromMilliseconds(500));
        }

        /// <summary>
        /// 
        /// </summary>
        private void TripTriggerInSyncWithTransition()
        {
            // First trip the trigger.
            TestTrigger(this, new EventArgs());

            // Now this will unblock the transition EventHandler so that it can set the waithandle.
            _testPreparedForTransition.Set();
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
