using System;
using System.Collections.Generic;
using System.Text.Json;
using ACMESharp.Protocol.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Services;
using Mock = PKISharp.WACS.UnitTests.Mock.Services;

namespace PKISharp.WACS.UnitTests.Tests.RenewalTests
{
    [TestClass]
    public class DueDateRuntimeServiceTests
    {
        private static readonly DateTime ValidFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ValidUntil = ValidFrom.AddDays(90);
        private readonly DueDateRuntimeService _service = new(new Mock.MockSettingsService(), new Mock.LogService(), new Mock.InputService(new List<string>()));

        [TestMethod]
        [DataRow("""{"suggestedWindow":{"start":"2026-01-02T00:00:00Z","end":"2026-01-02T00:00:00Z"}}""")]
        [DataRow("""{"suggestedWindow":{"start":"2026-01-03T00:00:00Z","end":"2026-01-02T00:00:00Z"}}""")]
        [DataRow("""{"suggestedWindow":{"start":"2026-01-02T00:00:00Z","end":"2026-01-01T19:00:00-05:00"}}""")]
        [DataRow("""{"suggestedWindow":{"start":"2026-01-02T00:00:00Z","end":null}}""")]
        [DataRow("""{"suggestedWindow":{"start":null,"end":"2026-01-02T00:00:00Z"}}""")]
        [DataRow("""{"suggestedWindow":null}""")]
        public void InvalidServerWindowUsesClientSchedule(string response)
        {
            var expected = _service.ComputeDueDate(ValidFrom, ValidUntil);
            var actual = _service.ComputeDueDate(ValidFrom, ValidUntil, JsonSerializer.Deserialize<AcmeRenewalInfo>(response));

            Assert.AreEqual(expected.Start, actual.Start, "Renewal start");
            Assert.AreEqual(expected.End, actual.End, "Renewal end");
            Assert.AreEqual(expected.Source, actual.Source, "Renewal schedule source");
        }

        [TestMethod]
        public void ValidEarlierServerWindowStillApplies()
        {
            var window = new AcmeRenewalInfoSuggestedWindow
            {
                Start = ValidFrom.AddDays(10),
                End = ValidFrom.AddDays(11)
            };

            var actual = _service.ComputeDueDate(ValidFrom, ValidUntil, new AcmeRenewalInfo { SuggestedWindow = window });

            Assert.AreEqual(window.Start.Value, actual.Start);
            Assert.AreEqual(window.End.Value, actual.End);
            Assert.AreEqual("ri-ri", actual.Source);
        }
    }
}
