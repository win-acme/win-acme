using System;
using PKISharp.WACS.Services;
using Serilog;
using Serilog.Core;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    internal class LogService : ILogService
    {
        private Logger _logger;

        public LogService()
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: " [{Level:u4}] {Message:l}{NewLine}{Exception}")
                .CreateLogger();
        }

        public bool Dirty { get; set; }
        public void Debug(string message, params object[] items)
        {
            _logger.Debug(message, items);
        }
        public void Error(Exception ex, string message, params object[] items)
        {
            _logger.Error(ex, message, items);
        }
        public void Error(string message, params object[] items)
        {
            _logger.Error(message, items);
        }
        public void Information(bool asEvent, string message, params object[] items)
        {
            _logger.Information(message, items);
        }
        public void Information(string message, params object[] items)
        {
            _logger.Information(message, items);
        }
        public void SetVerbose() { }
        public void Verbose(string message, params object[] items)
        {
            _logger.Verbose(message, items);
        }
        public void Warning(string message, params object[] items)
        {
            _logger.Warning(message, items);
        }
    }
}
