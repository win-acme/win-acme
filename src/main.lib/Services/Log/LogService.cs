using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Settings.Configuration;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PKISharp.WACS.Services
{
    public class LogService : ILogService
    {
        private readonly Logger? _screenLogger;
        private readonly Logger? _debugScreenLogger;
        private readonly Logger? _eventLogger;
        private Logger? _diskLogger;
        private readonly Logger? _notificationLogger;
        private readonly LoggingLevelSwitch _levelSwitch;
        private readonly List<MemoryEntry> _lines = new();

        public bool Dirty { get; set; }
        private string ConfigurationPath { get; }

        public IEnumerable<MemoryEntry> Lines => _lines.AsEnumerable();
        public void Reset() => _lines.Clear();

        public LogService(bool verbose)
        {
            // Custom configuration support
            ConfigurationPath = Path.Combine(VersionService.BasePath, "serilog.json");
#if DEBUG
            var initialLevel = LogEventLevel.Debug;
#else
            var initialLevel = LogEventLevel.Information;
#endif
            if (verbose)
            {
                initialLevel = LogEventLevel.Verbose;
            }
            _levelSwitch = new LoggingLevelSwitch(initialMinimumLevel: initialLevel);
            try
            {
                var theme = 
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    Environment.OSVersion.Version.Major == 10 ? 
                    (ConsoleTheme)AnsiConsoleTheme.Code : 
                    SystemConsoleTheme.Literate;

                _screenLogger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .Enrich.FromLogContext()
                    .Filter.ByIncludingOnly(x => { Dirty = true; return true; })
                    .WriteTo.Console(
                        outputTemplate: " {Message:l}{NewLine}", 
                        theme: theme)
                    .CreateLogger();
                _debugScreenLogger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .Enrich.FromLogContext()
                    .Filter.ByIncludingOnly(x => { Dirty = true; return true; })
                    .WriteTo.Console(
                        outputTemplate: " [{Level:u4}] {Message:l}{NewLine}{Exception}", 
                        theme: theme)
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" Error creating screen logger: {ex.Message} - {ex.StackTrace}");
                Console.ResetColor();
                Console.WriteLine();
                Environment.Exit(ex.HResult);
            }

            try
            {
                var _eventConfig = new ConfigurationBuilder()
                   .AddJsonFile(ConfigurationPath, true, true)
                   .Build();

                _eventLogger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .Enrich.FromLogContext()
                    .WriteTo.EventLog("win-acme", manageEventSource: true)
                    .ReadFrom.Configuration(_eventConfig, new ConfigurationReaderOptions(typeof(LogService).Assembly) { SectionName = "event" })
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                Warning("Error creating event logger: {ex}", ex.Message);
            }

            _notificationLogger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_levelSwitch)
                .Enrich.FromLogContext()
                .WriteTo.Memory(_lines)
                .CreateLogger();

            Debug("Logging at level {initialLevel}", initialLevel);
        }

        public void SetDiskLoggingPath(string path)
        {
            try
            {
                var defaultPath = path.TrimEnd('\\', '/') + "\\log-.txt";
                var defaultRollingInterval = RollingInterval.Day;
                var defaultRetainedFileCountLimit = 120;
                var fileConfig = new ConfigurationBuilder()
                   .AddJsonFile(ConfigurationPath, true, true)
                   .Build();

                foreach (var writeTo in fileConfig.GetSection("disk:WriteTo").GetChildren())
                {
                    if (writeTo.GetValue<string>("Name") == "File")
                    {
                        var pathSection = writeTo.GetSection("Args:path");
                        if (string.IsNullOrEmpty(pathSection.Value))
                        {
                            pathSection.Value = defaultPath;
                        }
                        var retainedFileCountLimit = writeTo.GetSection("Args:retainedFileCountLimit");
                        if (string.IsNullOrEmpty(retainedFileCountLimit.Value))
                        {
                            retainedFileCountLimit.Value = defaultRetainedFileCountLimit.ToString();
                        }
                        var rollingInterval = writeTo.GetSection("Args:rollingInterval");
                        if (string.IsNullOrEmpty(rollingInterval.Value))
                        {
                            rollingInterval.Value = ((int)defaultRollingInterval).ToString();
                        }
                    }
                }

                _diskLogger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                    .WriteTo.File(
                        defaultPath, 
                        rollingInterval: defaultRollingInterval,
                        retainedFileCountLimit: defaultRetainedFileCountLimit)
                    .ReadFrom.Configuration(fileConfig, new ConfigurationReaderOptions(typeof(LogService).Assembly) { SectionName = "disk" })
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                Warning("Error creating disk logger: {ex}", ex.Message);
            }
        }

        public void Verbose(string message, params object?[] items) => Verbose(LogType.Screen | LogType.Disk, message, items);

        public void Debug(string message, params object?[] items) => Debug(LogType.Screen | LogType.Disk, message, items);

        public void Warning(string message, params object?[] items) => Warning(LogType.All, message, items);

        public void Error(string message, params object?[] items) => Error(LogType.All, message, items);

        public void Error(Exception ex, string message, params object?[] items) => Error(LogType.All, ex, message, items);

        public void Information(string message, params object?[] items) => Information(LogType.Screen | LogType.Disk, message, items);

        public void Information(LogType logType, string message, params object?[] items) => _Information(logType, message, items);

        public void Verbose(LogType type, string message, params object?[] items) => Write(type, LogEventLevel.Verbose, message, items);

        private void Debug(LogType type, string message, params object?[] items) => Write(type, LogEventLevel.Debug, message, items);

        private void _Information(LogType type, string message, params object?[] items) => Write(type, LogEventLevel.Information, message, items);

        private void Warning(LogType type, string message, params object?[] items) => Write(type, LogEventLevel.Warning, message, items);

        private void Error(LogType type, string message, params object?[] items) => Write(type, LogEventLevel.Error, message, items);

        private void Error(LogType type, Exception ex, string message, params object?[] items) => Write(type, LogEventLevel.Error, ex, message, items);

        private void Write(LogType type, LogEventLevel level, string message, params object?[] items) => Write(type, level, null, message, items);

        private void Write(LogType type, LogEventLevel level, Exception? ex, string message, params object?[] items)
        {
            if (type.HasFlag(LogType.Screen))
            {
                if (_screenLogger != null && _levelSwitch.MinimumLevel >= LogEventLevel.Information)
                {
                    _screenLogger.Write(level, ex, message, items);
                }
                else if (_debugScreenLogger != null)
                {
                    _debugScreenLogger.Write(level, ex, message, items);
                }
                if (_notificationLogger != null)
                {
                    _notificationLogger.Write(level, ex, message, items);
                }
            }
            if (_eventLogger != null && type.HasFlag(LogType.Event))
            {
                _eventLogger.Write(level, ex, message, items);
            }
            if (_diskLogger != null && type.HasFlag(LogType.Disk))
            {
                _diskLogger.Write(level, ex, message, items);
            }
        }

    }
}
