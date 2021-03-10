using PKISharp.WACS.Services;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public class MemoryEntry
    {
        public MemoryEntry(LogEventLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public LogEventLevel Level { get; set; }
        public string Message { get; set; }
    }

    class MemorySink : ILogEventSink
    {
        private readonly IFormatProvider? _formatProvider;
        private readonly List<MemoryEntry> _list;

        public MemorySink(List<MemoryEntry> list, IFormatProvider? formatProvider = null)
        {
            _formatProvider = formatProvider;
            _list = list;
        }

        public void Emit(LogEvent logEvent) => _list.Add(new MemoryEntry(logEvent.Level, logEvent.RenderMessage(_formatProvider)));
    }
}

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.Memory() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationStackifyExtensions
    {
        public static LoggerConfiguration Memory(this LoggerSinkConfiguration loggerConfiguration, List<MemoryEntry> target, IFormatProvider? formatProvider = null) => loggerConfiguration.Sink(new MemorySink(target, formatProvider));
    }
}
