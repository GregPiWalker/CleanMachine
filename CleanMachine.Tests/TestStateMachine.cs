using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net;
using CleanMachine.Behavioral;
using CleanMachine.Interfaces;
using System.Threading;

namespace CleanMachine.Tests
{
    [TestClass]
    public class TestStateMachine
    {
        private static readonly ILog _logger = LogManager.GetLogger("Test Logger");

        [TestMethod]
        [TestCategory("Integration")]
        public void Signal_GivenTriggeredTransition_TransitionNotTraversed()
        {
            var uut = StateMachineFactory.Create<DummyState>("Test StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            TestBuilder.BuildOneWayMachineWithTriggers(harness.Machine, harness);

            uut.CompleteEdit();
            Assert.AreEqual(DummyState.One, uut.CurrentState);

            bool signalResult = uut.Signal(new DataWaypoint(this, "Test method"));
            Assert.IsFalse(signalResult, "A transition was traversed but should not have been.");
            Assert.AreEqual(DummyState.One, uut.CurrentState);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void Signal_GivenPassiveTransitionAndAutoAdvanceOff_SingleTransitionTraversed()
        {
            var uut = StateMachineFactory.Create<DummyState>("Test StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            TestBuilder.BuildOneWayPassiveMachine(harness.Machine);

            uut.AutoAdvance = false;
            uut.CompleteEdit();
            Assert.AreEqual(DummyState.One, uut.CurrentState);

            bool signalResult = uut.Signal(new DataWaypoint(this, "Test method"));
            Assert.IsTrue(signalResult, "Failed to signal the machine under test.");
            Assert.AreEqual(DummyState.Two, uut.CurrentState);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void Signal_GivenPassiveTransitionAndAutoAdvanceOn_MultipleTransitionTraversed()
        {
            var uut = StateMachineFactory.Create<DummyState>("Test StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            TestBuilder.BuildOneWayPassiveMachine(harness.Machine);

            uut.AutoAdvance = true;
            uut.CompleteEdit();
            Assert.AreEqual(DummyState.One, uut.CurrentState);

            bool signalResult = uut.Signal(new DataWaypoint(this, "Test method"));
            Assert.IsTrue(signalResult, "Failed to signal the machine under test.");
            Assert.AreEqual(DummyState.Three, uut.CurrentState);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SignalAsync_GivenPassiveTransitionAndAutoAdvanceOn_TransitionsTraversedOnTaskThread()
        {
            var uut = StateMachineFactory.Create<DummyState>("Test StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            List<int> actuals = new List<int>();
            TestBuilder.BuildOneWayPassiveMachine(harness.Machine);
            TestBuilder.AddEffectToAll(uut, "Record Thread ID", c =>
            {
                actuals.Add(Thread.CurrentThread.ManagedThreadId);
            });

            uut.AutoAdvance = true;
            uut.CompleteEdit();

            var signalTask = uut.SignalAsync(new DataWaypoint(this, "Test method"));
            bool signalResult = signalTask.Result;
            Assert.IsTrue(signalResult, "Failed to signal the machine under test.");
            Assert.AreNotEqual(0, actuals.Count, "No EFFECTS were executed.");
            foreach (var threadId in actuals)
            {
                Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, threadId, "A transition occurred on the main thread.");
            }
            //TODO: assert Task worker thread was used
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SignalAsync_GivenPassiveTransitionAndAutoAdvanceOn_TransitionsTraversedOnTriggerScheduler()
        {
            var uut = Behavioral.StateMachineFactory.CreateTriggerAsync<DummyState>("Test StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            List<int> actuals = new List<int>();
            TestBuilder.BuildOneWayPassiveMachine(harness.Machine);
            TestBuilder.AddEffectToAll(uut, "Record Thread ID", c =>
            {
                actuals.Add(Thread.CurrentThread.ManagedThreadId);
            });

            uut.AutoAdvance = true;
            uut.CompleteEdit();

            var signalTask = uut.SignalAsync(new DataWaypoint(this, "Test method"));
            bool signalResult = signalTask.Result;
            Assert.IsTrue(signalResult, "Failed to signal the machine under test.");
            Assert.AreNotEqual(0, actuals.Count, "No EFFECTS were executed.");
            var schedulerId = harness.GetTriggerSchedulerThreadId();
            foreach (var threadId in actuals)
            {
                Assert.AreEqual(schedulerId, threadId, "A transition did not occur on the trigger scheduler thread.");
                Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, threadId, "A transition occurred on the main thread.");
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void TryTransitionTo_GivenPassiveTransitionAndAutoAdvanceOff_WhenInitialStateOne_TransitionsToStateTwo()
        {
            var uut = StateMachineFactory.Create<DummyState>("Test StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            TestBuilder.BuildOneWayPassiveMachine(harness.Machine);
            uut.AutoAdvance = false;
            uut.CompleteEdit();

            uut.TryTransitionTo(DummyState.Two, this);
            Assert.AreEqual(DummyState.Two, uut.CurrentState);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void TryTransition_GivenPassiveTransitionAndAutoAdvanceOff_WhenInitialStateOne_TransitionsToStateTwo()
        {
            var uut = StateMachineFactory.Create<DummyState>("Test StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            TestBuilder.BuildOneWayPassiveMachine(harness.Machine);
            uut.AutoAdvance = false;
            uut.CompleteEdit();

            uut.TryTransition(this);
            Assert.AreEqual(DummyState.Two, uut.CurrentState);
        }
    }
}
