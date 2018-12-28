using PKISharp.WACS.Services;
using PKISharp.WACS.Extensions;
using System;

namespace PKISharp.WACS
{
    public class DnsScriptOptions
    {
        public string CreateScript { get; set; }
        public string DeleteScript { get; set; }

        public DnsScriptOptions() { }

        public DnsScriptOptions(IOptionsService options, ILogService log)
        {
            CreateScript = options.TryGetRequiredOption(nameof(options.Options.DnsCreateScript), options.Options.DnsCreateScript);
            if (!CreateScript.ValidFile(log))
            {
                throw new ArgumentException(nameof(options.Options.DnsCreateScript));
            }
            DeleteScript = options.TryGetRequiredOption(nameof(options.Options.DnsDeleteScript), options.Options.DnsDeleteScript);
            if (!DeleteScript.ValidFile(log))
            {
                throw new ArgumentException(nameof(options.Options.DnsDeleteScript));
            }
        }

        public DnsScriptOptions(IOptionsService options, IInputService input, ILogService log)
        {
            do
            {
                CreateScript = options.TryGetOption(options.Options.DnsCreateScript, input, "Path to script that creates DNS records. Parameters passed are the hostname, record name and token");
            }
            while (!CreateScript.ValidFile(log));

            do
            {
                DeleteScript = options.TryGetOption(options.Options.DnsDeleteScript, input, "Path to script that deletes DNS records. Parameters passed are the hostname and record name");
            }
            while (!DeleteScript.ValidFile(log));
        }
    }
}