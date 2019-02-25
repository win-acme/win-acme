using System;
using System.Collections.Generic;
using System.Linq;
using PKISharp.WACS.Services;
using Serilog;
using Serilog.Core;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    internal class LogService : ILogService
    {
        private Logger _logger;
        private bool _throwErrors;
        public List<string> DebugMessages { get; } = new List<string>();
        public List<string> WarningMessages { get; } = new List<string>();
        public List<string> InfoMessages { get; } = new List<string>();
        public List<string> ErrorMessages { get; } = new List<string>();
        public List<string> VerboseMessages { get; } = new List<string>();

        public LogService(bool throwErrors)
        {
            _throwErrors = throwErrors;
            _logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: " [{Level:u4}] {Message:l}{NewLine}{Exception}")
                .CreateLogger();
        }

        public bool Dirty { get; set; }
        public void Debug(string message, params object[] items)
        {
            DebugMessages.Add(message);
            _logger.Debug(message, items);
        }
        public void Error(Exception ex, string message, params object[] items)
        {
            ErrorMessages.Add(message);
            _logger.Error(ex, message, items);
            if (_throwErrors)
            {
                throw ex;
            }
        }
        public void Error(string message, params object[] items)
        {
            ErrorMessages.Add(message);
            _logger.Error(message, items);
            if (_throwErrors)
            {
                throw new Exception(message);
            }
        }
        public void Information(bool asEvent, string message, params object[] items)
        {
            InfoMessages.Add(message);
            _logger.Information(message, items);
        }

        public void Information(string message, params object[] items)
        {
            InfoMessages.Add(message);
            _logger.Information(message, items);
        }
        public void SetVerbose() { }
        public void Verbose(string message, params object[] items)
        {
            VerboseMessages.Add(message);
            _logger.Verbose(message, items);
        }
        public void Warning(string message, params object[] items)
        {
            WarningMessages.Add(message);
            _logger.Warning(message, items);
        }
    }
}
