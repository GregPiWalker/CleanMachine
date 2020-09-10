using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net;
using CleanMachine.Behavioral;

namespace CleanMachine.Tests
{
    [TestClass]
    public class TestStateMachine
    {
        private static readonly ILog _logger = LogManager.GetLogger("Test Logger");

        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        [TestMethod]
        public void TestMethod1()
        {
            var uut = StateMachineFactory.Create<DummyState>("Demo StateMachine", _logger);
            //
            // TODO: Add test logic here
            //
            Assert.Inconclusive("TODO impl");
        }
    }
}
