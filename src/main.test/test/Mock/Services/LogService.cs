using System;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    internal class LogService : ILogService
    {
        public bool Dirty { get; set; }
        public void Debug(string message, params object[] items) { }
        public void Error(Exception ex, string message, params object[] items) { }
        public void Error(string message, params object[] items) { }
        public void Information(bool asEvent, string message, params object[] items) { }
        public void Information(string message, params object[] items) { }
        public void SetVerbose() { }
        public void Verbose(string message, params object[] items) { }
        public void Warning(string message, params object[] items) { }
    }
}
