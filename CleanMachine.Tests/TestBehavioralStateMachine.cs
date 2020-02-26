using System;
using CleanMachine.Generic;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CleanMachine.Tests
{
    [TestClass]
    public class TestBehavioralStateMachine
    {
        private static readonly ILog _logger = LogManager.GetLogger("Threading Test Logger");

        [TestInitialize]
        public void Init()
        { }

        [TestMethod]
        [TestCategory("Integration")]
        public void ChangeState_GivenFullyAsync_DoBehaviorIsAsync()
        {
            var uut = StateMachineFactory.CreateAsync<DummyState>("Demo StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            harness.BuildOneWayMachine();

            // The DO behavior should be able to complete its work independent of the current waiting thread.
            Assert.IsTrue(harness.WaitUntilAsyncDoBehavior(TimeSpan.FromSeconds(1000)), "Waited too long for DO behavior execution.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void ChangeState_GivenFullyAsync_TransitionIsAsync()
        {
            var uut = StateMachineFactory.CreateAsync<DummyState>("Demo StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            harness.BuildOneWayMachine();

            // The transition should be able to complete its work and signal success independent of the current waiting thread.
            Assert.IsTrue(harness.WaitUntilAsyncTransitionSuccess(TimeSpan.FromSeconds(1)), "Waited too long for a state transition.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void ChangeState_GivenPartialAsync_DoBehaviorIsNotAsync()
        {
            var uut = StateMachineFactory.CreatePartialAsync<DummyState>("Demo StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            harness.BuildOneWayMachine();

            // The DO behavior should be blocked from completing its work because the current thread is on the transition,
            // which is waiting on the DO behavior.
            Assert.IsFalse(harness.WaitUntilAsyncDoBehavior(TimeSpan.FromSeconds(1)), "DO behavior executed asynchronously.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void ChangeState_GivenPartialAsync_TransitionIsAsync()
        {
            var uut = StateMachineFactory.CreatePartialAsync<DummyState>("Demo StateMachine", _logger);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            harness.BuildOneWayMachine();

            Assert.IsTrue(harness.WaitUntilAsyncTransitionSuccess(TimeSpan.FromSeconds(1)), "Waited too long for a state transition.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void ChangeState_GivenPartialSync_WhereBehaviorIsNotAsync_DoBehaviorIsNotAsync()
        {
            var uut = StateMachineFactory.Create<DummyState>("Demo StateMachine", _logger, false);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            harness.BuildOneWayMachine();

            Assert.IsFalse(harness.WaitUntilAsyncDoBehavior(TimeSpan.FromSeconds(1)), "DO behavior executed asynchronously.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void ChangeState_GivenPartialSync_WhereBehaviorIsAsync_DoBehaviorIsAsync()
        {
            var uut = StateMachineFactory.Create<DummyState>("Demo StateMachine", _logger, true);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            harness.BuildOneWayMachine();

            // The DO behavior should be able to complete its work independent of the current waiting thread.
            Assert.IsTrue(harness.WaitUntilAsyncDoBehavior(TimeSpan.FromSeconds(1)), "Waited too long for DO behavior execution.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void ChangeState_GivenPartialSync_WhereBehaviorIsNotAsync_TransitionIsNotAsync()
        {
            var uut = StateMachineFactory.Create<DummyState>("Demo StateMachine", _logger, false);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            harness.BuildOneWayMachine();

            // The transition should be blocked from completing its work because the transition happens on the current thread.
            Assert.IsFalse(harness.WaitUntilAsyncTransitionSuccess(TimeSpan.FromSeconds(1)), "Transition executed asynchronously.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void ChangeState_GivenPartialSync_WhereBehaviorIsAsync_TransitionIsNotAsync()
        {
            var uut = StateMachineFactory.Create<DummyState>("Demo StateMachine", _logger, true);
            var harness = new StateMachineTestHarness<DummyState>(uut, DummyState.One.ToString());
            harness.BuildOneWayMachine();
            
            // The transition should be blocked from completing its work because the transition happens on the current thread.
            Assert.IsFalse(harness.WaitUntilAsyncTransitionSuccess(TimeSpan.FromSeconds(1)), "Transition executed asynchronously.");
        }
    }
}
