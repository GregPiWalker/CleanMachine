using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CleanMachine.Tests
{
    [TestClass]
    public class TestStateMachineFactory
    {
        private static readonly ILog _logger = LogManager.GetLogger("Factory Test Logger");

        [TestMethod]
        [TestCategory("Integration")]
        public void CreateAsync_TriggersHaveThread_AndBehaviorsHaveThread()
        {
            var uut = Behavioral.StateMachineFactory.CreateAsync<DummyState>("Demo StateMachine", _logger);
            Assert.IsTrue(uut.HasBehaviorScheduler);
            Assert.IsTrue(uut.HasTriggerScheduler);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void CreateTriggerAsync_TriggersHaveThread_AndBehaviorsDoNotHaveThread()
        {
            var uut = Behavioral.StateMachineFactory.CreateTriggerAsync<DummyState>("Demo StateMachine", _logger);
            Assert.IsFalse(uut.HasBehaviorScheduler);
            Assert.IsTrue(uut.HasTriggerScheduler);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void CreateBehaviorAsync_GivenPartialSync_WhereBehaviorIsAsync_DoBehaviorIsAsync()
        {
            var uut = Behavioral.StateMachineFactory.CreateBehaviorAsync<DummyState>("Demo StateMachine", _logger);
            Assert.IsTrue(uut.HasBehaviorScheduler);
            Assert.IsFalse(uut.HasTriggerScheduler);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void Create_TriggersDoNotHaveThread_AndBehaviorsDoNotHaveThread_AndHasSynchronizer()
        {
            var uut = StateMachineFactory.Create<DummyState>("Demo StateMachine", _logger);
            Assert.IsFalse(uut.HasBehaviorScheduler);
            Assert.IsFalse(uut.HasTriggerScheduler);
            Assert.IsNotNull(uut._synchronizer);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void Create_TriggersDoNotHaveThread_AndBehaviorsDoNotHaveThread_AndHasSharedSynchronizer()
        {
            object sync = new object();
            var uut = StateMachineFactory.Create<DummyState>("Demo StateMachine", _logger, sync);
            Assert.IsFalse(uut.HasBehaviorScheduler);
            Assert.IsFalse(uut.HasTriggerScheduler);
            Assert.AreEqual(uut._synchronizer, sync);
        }
    }
}
