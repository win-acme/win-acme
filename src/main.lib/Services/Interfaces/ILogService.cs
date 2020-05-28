using System;

namespace PKISharp.WACS.Services
{

    [Flags]
    public enum LogType
    {
        None = 0,
        Screen = 1,
        Event = 2,
        Disk = 4,
        Notification = 8,
        All = int.MaxValue
    }

    public interface ILogService
    {
        bool Dirty { get; set; }

        void Debug(string message, params object?[] items);
        void Error(Exception ex, string message, params object?[] items);
        void Error(string message, params object?[] items);
        void Information(string message, params object?[] items);
        void Information(LogType logType, string message, params object?[] items);
        void SetVerbose();
        void Verbose(string message, params object?[] items);
        void Verbose(LogType logType, string message, params object?[] items);
        void Warning(string message, params object?[] items);
    }
}