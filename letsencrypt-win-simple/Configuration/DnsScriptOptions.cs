using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple
{
    public class DnsScriptOptions
    {
        public string CreateScript { get; set; }
        public string DeleteScript { get; set; }

        public DnsScriptOptions() { }

        public DnsScriptOptions(Options options)
        {
            CreateScript = options.TryGetRequiredOption(nameof(options.DnsCreateScript), options.DnsCreateScript);
            DeleteScript = options.TryGetRequiredOption(nameof(options.DnsDeleteScript), options.DnsDeleteScript);
        }

        public DnsScriptOptions(Options options, InputService input)
        {
            CreateScript = options.TryGetOption(options.DnsCreateScript, input, "Path to script that creates DNS records. Parameters passed are the hostname, record name and token");
            DeleteScript = options.TryGetOption(options.DnsDeleteScript, input, "Path to script that deletes DNS records. Parameters passed are the hostname and record name");
        }
    }
}