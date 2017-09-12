using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace LetsEncrypt.ACME.Simple.Services
{
    class LogService
    {
        private Logger _screenLogger;
        private Logger _eventLogger;
        private LoggingLevelSwitch _levelSwitch;

        [Flags]
        public enum LogType
        {
            Screen = 1,
            Event = 2,
            Both = Screen | Event
        }

        public LogService()
        {
#if DEBUG
            var initialLevel = LogEventLevel.Verbose;
#else
            var initialLevel = LogEventLevel.Information;
#endif
            _levelSwitch = new LoggingLevelSwitch(initialMinimumLevel: initialLevel);
            try
            {
                _screenLogger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .WriteTo.Console(outputTemplate: "[{Level:u4}] {Message:l}{NewLine}{Exception}", theme: SystemConsoleTheme.Literate)
                    .ReadFrom.AppSettings()
                    .CreateLogger();

                _eventLogger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .WriteTo.EventLog("letsencrypt_win_simple", manageEventSource: true)
                    .ReadFrom.AppSettings("event")
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

        public void SetVerbose()
        {
            _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
        }

        internal void Verbose(string message, params object[] items)
        {
            Verbose(LogType.Screen, message, items);
        }

        internal void Debug(string message, params object[] items)
        {
            Debug(LogType.Screen, message, items);
        }

        internal void Information(string message, params object[] items)
        {
            Information(false, message, items);
        }

        internal void Warning(string message, params object[] items)
        {
            Warning(LogType.Screen | LogType.Event, message, items);
        }

        internal void Error(string message, params object[] items)
        {
            Error(LogType.Screen | LogType.Event, message, items);
        }

        internal void Error(Exception ex, string message, params object[] items)
        {
            Error(LogType.Screen | LogType.Event, ex, message, items);
        }

        internal void Information(bool asEvent, string message, params object[] items)
        {
            var type = LogType.Screen;
            if (asEvent)
            {
                type = type | LogType.Event;
            }
            Information(type, message, items);
        }

        internal void Verbose(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Verbose, message, items);
        }

        internal void Debug(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Debug, message, items);
        }

        internal void Information(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Information, message, items);
        }

        internal void Warning(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Warning, message, items);
        }

        internal void Error(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Error, message, items);
        }

        internal void Error(LogType type, Exception ex, string message, params object[] items)
        {
            Write(type, LogEventLevel.Error, ex, message, items);
        }

        internal void Write(LogType type, LogEventLevel level, string message, params object[] items)
        {
            Write(type, level, null, message, items);
        }

        internal void Write(LogType type, LogEventLevel level, Exception ex, string message, params object[] items)
        {
            if ((type & LogType.Screen) == LogType.Screen)
            {
                _screenLogger.Write(level, ex, message, items);
            }
            if ((type & LogType.Event) == LogType.Event)
            {
                _eventLogger.Write(level, ex, message, items);
            }
        }
    }
}
