using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreOptionsFactory : StorePluginOptionsFactory<CertificateStore, CertificateStoreOptions>
    {
        private readonly IArgumentsService _arguments;
        private readonly IIISClient _iisClient;
        private readonly ISettingsService _settingsService;

        public CertificateStoreOptionsFactory(
            IUserRoleService userRoleService, 
            IArgumentsService arguments,
            ISettingsService settings,
            IIISClient iisClient)
        {
            _arguments = arguments;
            _iisClient = iisClient;
            _settingsService = settings;
            Disabled = CertificateStore.Disabled(userRoleService);
        }

        public override async Task<CertificateStoreOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            var ret = await Default();
            if (ret != null &&
                string.IsNullOrEmpty(ret.StoreName) &&
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

        public override async Task<CertificateStoreOptions?> Default()
        {
            var args = _arguments.GetArguments<CertificateStoreArguments>();
            var ret = new CertificateStoreOptions {
                StoreName = args?.CertificateStore,
                KeepExisting = args?.KeepExisting ?? false,
                AclFullControl = args?.AclFullControl.ParseCsv()
            };
            return ret;
        }
    }
}
