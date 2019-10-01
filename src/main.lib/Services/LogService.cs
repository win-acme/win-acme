using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Diagnostics;
using System.IO;

namespace PKISharp.WACS.Services
{
    public class LogService : ILogService
    {
        private readonly Logger _screenLogger;
        private readonly Logger _eventLogger;
        private Logger _diskLogger;
        private readonly LoggingLevelSwitch _levelSwitch;
        public bool Dirty { get; set; }
        private IConfigurationRoot ConfigurationRoot { get; }

        public LogService()
        {
            // Custom configuration support
            var installDir = new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName;
            ConfigurationRoot = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(installDir, "serilog.json"), true, true)
                .Build();
#if DEBUG
            var initialLevel = LogEventLevel.Debug;
#else
            var initialLevel = LogEventLevel.Information;
#endif
            _levelSwitch = new LoggingLevelSwitch(initialMinimumLevel: initialLevel);
            try
            {
                _screenLogger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .Enrich.FromLogContext()
                    .Filter.ByIncludingOnly(x => { Dirty = true; return true; })
                    .WriteTo.Console(outputTemplate: " [{Level:u4}] {Message:l}{NewLine}{Exception}", theme: SystemConsoleTheme.Literate)
                    .ReadFrom.Configuration(ConfigurationRoot, "screen")
                    .CreateLogger();

                _eventLogger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .Enrich.FromLogContext()
                    .WriteTo.EventLog("win-acme", manageEventSource: true)
                    .ReadFrom.Configuration(ConfigurationRoot, "event")
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" Error while creating logger: {ex.Message} - {ex.StackTrace}");
                Console.ResetColor();
                Console.WriteLine();
                Environment.Exit(ex.HResult);
            }
            Log.Debug("The global logger has been configured");
        }

        public void SetDiskLoggingPath(string path)
        {
            _diskLogger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_levelSwitch)
                .Enrich.FromLogContext()
                .WriteTo.File(path.TrimEnd('\\', '/') + "\\log-.txt", rollingInterval: RollingInterval.Day)
                .ReadFrom.Configuration(ConfigurationRoot, "disk")
                .CreateLogger();
        }

        public void SetVerbose()
        {
            _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
            Verbose("Verbose mode logging enabled");
        }

        public void Verbose(string message, params object[] items) => Verbose(LogType.Screen, message, items);

        public void Debug(string message, params object[] items) => Debug(LogType.Screen, message, items);

        public void Warning(string message, params object[] items) => Warning(LogType.Screen | LogType.Event, message, items);

        public void Error(string message, params object[] items) => Error(LogType.Screen | LogType.Event, message, items);

        public void Error(Exception ex, string message, params object[] items) => Error(LogType.Screen | LogType.Event, ex, message, items);

        public void Information(string message, params object[] items) => Information(LogType.Screen, message, items);

        public void Information(LogType logType, string message, params object[] items) => _Information(logType, message, items);

        private void Verbose(LogType type, string message, params object[] items) => Write(type, LogEventLevel.Verbose, message, items);

        private void Debug(LogType type, string message, params object[] items) => Write(type, LogEventLevel.Debug, message, items);

        private void _Information(LogType type, string message, params object[] items) => Write(type, LogEventLevel.Information, message, items);

        private void Warning(LogType type, string message, params object[] items) => Write(type, LogEventLevel.Warning, message, items);

        private void Error(LogType type, string message, params object[] items) => Write(type, LogEventLevel.Error, message, items);

        private void Error(LogType type, Exception ex, string message, params object[] items) => Write(type, LogEventLevel.Error, ex, message, items);

        private void Write(LogType type, LogEventLevel level, string message, params object[] items) => Write(type, level, null, message, items);

        private void Write(LogType type, LogEventLevel level, Exception ex, string message, params object[] items)
        {
            if (type.HasFlag(LogType.Screen))
            {
                _screenLogger.Write(level, ex, message, items);
            }
            if (type.HasFlag(LogType.Event))
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
