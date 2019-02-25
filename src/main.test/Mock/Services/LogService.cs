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
        private readonly List<string> _debug = new List<string>();
        private readonly List<string> _warn = new List<string>();
        private readonly List<string> _info = new List<string>();
        private readonly List<string> _error = new List<string>();
        private readonly List<string> _verbose = new List<string>();

        public bool ContainsError(string like)
        {
            return _error.Any(x => x.Contains(like));
        }

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
            _debug.Add(message);
            _logger.Debug(message, items);
        }
        public void Error(Exception ex, string message, params object[] items)
        {
            _error.Add(string.Format(message, items));
            _logger.Error(ex, message, items);
            if (_throwErrors)
            {
                throw ex;
            }
        }
        public void Error(string message, params object[] items)
        {
            _error.Add(string.Format(message, items));
            _logger.Error(message, items);
            if (_throwErrors)
            {
                throw new Exception(message);
            }
        }
        public void Information(bool asEvent, string message, params object[] items)
        {
            _logger.Information(message, items);
            _info.Add(message);
        }
        public void Information(string message, params object[] items)
        {
            _logger.Information(message, items);
            _info.Add(message);
        }
        public void SetVerbose() { }
        public void Verbose(string message, params object[] items)
        {
            _logger.Verbose(message, items);
            _verbose.Add(message);
        }
        public void Warning(string message, params object[] items)
        {
            _logger.Warning(message, items);
            _verbose.Add(message);
        }
    }
}
