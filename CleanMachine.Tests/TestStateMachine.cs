using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net;
using CleanMachine.Behavioral;
using CleanMachine.Interfaces;

namespace CleanMachine.Tests
{
    [TestClass]
    public class TestStateMachine
    {
        private static readonly ILog _logger = LogManager.GetLogger("Test Logger");

        [TestMethod]
        [TestCategory("Integration")]
        public void Signal_GivenPassiveTransitionAndAutoAdvanceOn_TransitionTraversed()
        {
            var uut = StateMachineFactory.Create<DummyState>("Test StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            TestBuilder.BuildOneWayMachineWithTriggers(harness.Machine, harness);

            uut.AutoAdvance = true;
            uut.CompleteEdit();
            var result = uut.FindState(DummyState.One).Name.ToEnum<DummyState>();
            Assert.AreEqual(uut.CurrentState, result);

            Assert.IsFalse(uut.Signal(new DataWaypoint(this, "Test method")), "Failed to signal the machine under test.");
            result = uut.FindState(DummyState.One).Name.ToEnum<DummyState>();
            Assert.AreEqual(uut.CurrentState, result);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void Signal_GivenPassiveTransitionAndAutoAdvanceOff_TransitionNotTraversed()
        {
            var uut = StateMachineFactory.Create<DummyState>("Test StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            TestBuilder.BuildOneWayPassiveMachine(harness.Machine);

            uut.AutoAdvance = false;
            uut.CompleteEdit();
            var result = uut.FindState(DummyState.One).Name.ToEnum<DummyState>();
            Assert.AreEqual(uut.CurrentState, result);

            Assert.IsTrue(uut.Signal(new DataWaypoint(this, "Test method")), "Failed to signal the machine under test.");
            result = uut.FindState(DummyState.Two).Name.ToEnum<DummyState>();
            Assert.AreEqual(uut.CurrentState, result);
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
