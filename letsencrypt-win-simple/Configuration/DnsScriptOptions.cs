using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple
{
    public class DnsScriptOptions
    {
        public string CreateScript { get; set; }
        public string DeleteScript { get; set; }

        public DnsScriptOptions() { }

        public DnsScriptOptions(OptionsService options)
        {
            CreateScript = options.TryGetRequiredOption(nameof(options.Options.DnsCreateScript), options.Options.DnsCreateScript);
            DeleteScript = options.TryGetRequiredOption(nameof(options.Options.DnsDeleteScript), options.Options.DnsDeleteScript);
        }

        public DnsScriptOptions(OptionsService options, InputService input)
        {
            CreateScript = options.TryGetOption(options.Options.DnsCreateScript, input, "Path to script that creates DNS records. Parameters passed are the hostname, record name and token");
            DeleteScript = options.TryGetOption(options.Options.DnsDeleteScript, input, "Path to script that deletes DNS records. Parameters passed are the hostname and record name");
        }
    }
}