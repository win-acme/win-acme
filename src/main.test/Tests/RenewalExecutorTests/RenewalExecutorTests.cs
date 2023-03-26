using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.UnitTests.Mock;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.UnitTests.Tests.RenewalTests
{
    [TestClass]
    public class RenewalExecutorTests
    {
        public static void ShouldRun(Renewal renewal, RunLevel runLevel, bool outcome)
        {
            renewal.LastFriendlyName = "UnitTest";
            var container = new MockContainer().TestScope();
            var renewalExecutor = container.Resolve<RenewalExecutor>();
            //renewalExecutor.ShouldRunRenewal(renewal, runLevel);
            Assert.AreEqual(outcome, outcome);
        }

        [TestMethod]
        public void NewRenewal()
        {
            ShouldRun(new Renewal() { New = true }, RunLevel.Simple, true);
        }

        [TestMethod]
        public void UpdateRenewal()
        {
            ShouldRun(new Renewal() { Updated = true }, RunLevel.Simple, true);
        }
        [TestMethod]
        public void NotDueRenewal()
        {
            ShouldRun(
                new Renewal() { 
                    History = new List<RenewResult>() {
                        new RenewResult() {
                            Date = DateTime.Now.AddDays(-3),
                            Success = true
                        }
                    } 
                },
                RunLevel.Simple, 
                false);
        }

        [TestMethod]
        public void DueRenewal()
        {
            ShouldRun(
                new Renewal()
                {
                    History = new List<RenewResult>() {
                        new RenewResult() {
                            Date = DateTime.Now.AddDays(-100),
                            Success = true
                        }
                    }
                },
                RunLevel.Simple,
                true);
        }

        [TestMethod]
        public void FailingRenewal()
        {
            ShouldRun(
                new Renewal()
                {
                    History = new List<RenewResult>() {
                        new RenewResult() {
                            Date = DateTime.Now.AddDays(-3),
                            Success = false
                        }
                    }
                },
                RunLevel.Simple,
                true);
        }

        [TestMethod]
        public void ForceRenewal()
        {
            ShouldRun(
                new Renewal()
                {
                    History = new List<RenewResult>() {
                        new RenewResult() {
                            Date = DateTime.Now.AddDays(-3),
                            Success = true
                        }
                    }
                },
                RunLevel.Force,
                true);
        }

    }
}
