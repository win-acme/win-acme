using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [IPlugin.Plugin<
        Rfc2136Options, Rfc2136OptionsFactory, 
        DnsValidationCapability, Rfc2136Json>
        ("ed5dc9d1-739c-4f6a-854f-238bf65b63ee",
        "Rfc2136",
        "Create verification records using dynamic updates")]
    internal sealed class Rfc2136 : DnsValidation<Rfc2136>
    {
        private readonly string? _key; 
        private readonly Rfc2136Options _options;

        public Rfc2136(
            LookupClientProvider dnsClient,
            IProxyService proxy,
            ILogService log,
            ISettingsService settings,
            SecretServiceManager ssm,
            Rfc2136Options options): base(dnsClient, log, settings)
        {
            _options = options;
            _key = ssm.EvaluateSecret(options.TsigKeySecret);
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            throw new NotImplementedException();
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            throw new NotImplementedException();
        }
    }
}