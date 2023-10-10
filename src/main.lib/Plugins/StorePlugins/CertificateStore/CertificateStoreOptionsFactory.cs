using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreOptionsFactory : PluginOptionsFactory<CertificateStoreOptions>
    {
        private readonly ArgumentsInputService _arguments;
        private readonly IIISClient _iisClient;
        private readonly ISettingsService _settingsService;

        public CertificateStoreOptionsFactory(
            ArgumentsInputService arguments,
            ISettingsService settings,
            IIISClient iisClient)
        {
            _arguments = arguments;
            _iisClient = iisClient;
            _settingsService = settings;
        }

        public override async Task<CertificateStoreOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            var ret = await Default();
            if (ret != null &&
                (await _arguments.GetString<CertificateStoreArguments>(x => x.CertificateStore).GetValue()) == null &&
                runLevel.HasFlag(RunLevel.Advanced))
            {
                var currentDefault = CertificateStore.DefaultStore(_settingsService, _iisClient);
                var choices = new List<Choice<string?>>();
                if (_iisClient.Version.Major > 8)
                {
                    choices.Add(Choice.Create<string?>(
                        "WebHosting", 
                        description: "[WebHosting] - Dedicated store for IIS"));
                }
                choices.Add(Choice.Create<string?>(
                        "My",
                        description: "[My] - General computer store (for Exchange/RDS)"));
                choices.Add(Choice.Create<string?>(
                    null, 
                    description: $"[Default] - Use global default, currently {currentDefault}",
                    @default: true));
                var choice = await inputService.ChooseFromMenu(
                    "Choose store to use, or type the name of another unlisted store",
                    choices,
                    other => Choice.Create<string?>(other));

                // final save
                ret.StoreName = string.IsNullOrWhiteSpace(choice) ? null : choice;
            }
            return ret;
        }

        private ArgumentResult<bool?> KeepExisting => _arguments.
            GetBool<CertificateStoreArguments>(x => x.KeepExisting).
            WithDefault(false).
            DefaultAsNull();

        private ArgumentResult<string?> StoreName => _arguments.
            GetString<CertificateStoreArguments>(x => x.CertificateStore).
            WithDefault(CertificateStore.DefaultStore(_settingsService, _iisClient)).
            DefaultAsNull();

        private ArgumentResult<string?> AclFullControl => _arguments.
            GetString<CertificateStoreArguments>(x => x.AclFullControl);

        private ArgumentResult<string?> AclRead => _arguments.
            GetString<CertificateStoreArguments>(x => x.AclRead);

        public override async Task<CertificateStoreOptions?> Default()
        {
            return new CertificateStoreOptions
            {
                StoreName = await StoreName.GetValue(),
                KeepExisting = await KeepExisting.GetValue(),
                AclFullControl = (await AclFullControl.GetValue()).ParseCsv(),
                AclRead = (await AclRead.GetValue()).ParseCsv()
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(CertificateStoreOptions options)
        {
            yield return (KeepExisting.Meta, options.KeepExisting);
            yield return (StoreName.Meta, options.StoreName);
            yield return (AclFullControl.Meta, options.AclFullControl);
            yield return (AclRead.Meta, options.AclRead);
        }
    }
}
