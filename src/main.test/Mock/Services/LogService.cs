using PKISharp.WACS.Services;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Concurrent;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    internal class LogService : ILogService
    {
        private readonly Logger _logger;
        private readonly bool _throwErrors;
        public ConcurrentQueue<string> DebugMessages { get; } = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> WarningMessages { get; } = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> InfoMessages { get; } = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> ErrorMessages { get; } = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> VerboseMessages { get; } = new ConcurrentQueue<string>();

        public LogService(bool throwErrors)
        {
            _throwErrors = throwErrors;
            _logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: " [{Level:u4}] {Message:l}{NewLine}{Exception}")
                .CreateLogger();
        }

        public bool Dirty { get; set; }
        public void Debug(string message, params object?[] items)
        {
            DebugMessages.Enqueue(message);
            _logger.Debug(message, items);
        }
        public void Error(Exception ex, string message, params object?[] items)
        {
            ErrorMessages.Enqueue(message);
            _logger.Error(ex, message, items);
            if (_throwErrors)
            {
                throw ex;
            }
        }
        public void Error(string message, params object?[] items)
        {
            ErrorMessages.Enqueue(message);
            _logger.Error(message, items);
            if (_throwErrors)
            {
                throw new Exception(message);
            }
        }

        public void Information(LogType logType, string message, params object?[] items)
        {
            InfoMessages.Enqueue(message);
            _logger.Information(message, items);
        }

        public void Information(string message, params object?[] items) => Information(LogType.All, message, items);

        public void SetVerbose() { }

        public void Verbose(string message, params object?[] items)
        {
            VerboseMessages.Enqueue(message);
            _logger.Verbose(message, items);
        }
        public void Verbose(LogType logType, string message, params object?[] items)
        {
            VerboseMessages.Enqueue(message);
            _logger.Verbose(message, items);
        }
        public void Warning(string message, params object?[] items)
        {
            WarningMessages.Enqueue(message);
            _logger.Warning(message, items);
        }
    }
}
