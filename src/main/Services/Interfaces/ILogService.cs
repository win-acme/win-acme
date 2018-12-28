using System;

namespace PKISharp.WACS.Services
{
    public interface ILogService
    {
        bool Dirty { get; set; }

        void Debug(string message, params object[] items);
        void Error(Exception ex, string message, params object[] items);
        void Error(string message, params object[] items);
        void Information(bool asEvent, string message, params object[] items);
        void Information(string message, params object[] items);
        void SetVerbose();
        void Verbose(string message, params object[] items);
        void Warning(string message, params object[] items);
    }
}