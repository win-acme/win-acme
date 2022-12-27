using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using static PKISharp.WACS.Services.ValidationOptionsService;
using Csr = PKISharp.WACS.Plugins.CsrPlugins;
using Installation = PKISharp.WACS.Plugins.InstallationPlugins;
using Order = PKISharp.WACS.Plugins.OrderPlugins;
using Target = PKISharp.WACS.Plugins.TargetPlugins;
using Validation = PKISharp.WACS.Plugins.ValidationPlugins;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Code generator for built-in types
    /// </summary>
    [JsonSerializable(typeof(PluginOptionsBase))]
    [JsonSerializable(typeof(CsrPluginOptions))]
    [JsonSerializable(typeof(Renewal))]
    [JsonSerializable(typeof(Target.ManualOptions))]
    [JsonSerializable(typeof(Target.IISOptions))]
    [JsonSerializable(typeof(Target.IISBindingOptions))]
    [JsonSerializable(typeof(Target.IISSiteOptions))]
    [JsonSerializable(typeof(Target.IISSitesOptions))]
    [JsonSerializable(typeof(Target.IISOptions))]
    [JsonSerializable(typeof(Target.CsrOptions))]
    [JsonSerializable(typeof(Validation.Dns.AcmeOptions))]
    [JsonSerializable(typeof(Validation.Dns.ManualOptions), TypeInfoPropertyName = "DnsManualOptions")]
    [JsonSerializable(typeof(Validation.Dns.ScriptOptions))]
    [JsonSerializable(typeof(Validation.Http.FileSystemOptions))]
    [JsonSerializable(typeof(Validation.Http.FtpOptions))]
    [JsonSerializable(typeof(Validation.Http.SelfHostingOptions))]
    [JsonSerializable(typeof(Validation.Http.SftpOptions))]
    [JsonSerializable(typeof(Validation.Http.WebDavOptions))]
    [JsonSerializable(typeof(Validation.Tls.SelfHostingOptions), TypeInfoPropertyName = "TlsSelfHostingOptions")]
    [JsonSerializable(typeof(Order.DomainOptions))]
    [JsonSerializable(typeof(Order.HostOptions))]
    [JsonSerializable(typeof(Order.SingleOptions))]
    [JsonSerializable(typeof(Csr.EcOptions))]
    [JsonSerializable(typeof(Csr.RsaOptions))]
    [JsonSerializable(typeof(Installation.IISFtpOptions))]
    [JsonSerializable(typeof(Installation.IISOptions), TypeInfoPropertyName = "InstallationIISOptions")]
    [JsonSerializable(typeof(Installation.ScriptOptions), TypeInfoPropertyName = "InstallationScriptOptions")]
    [JsonSerializable(typeof(NullInstallationOptions))]
    [JsonSerializable(typeof(NullStoreOptions))]
    [JsonSerializable(typeof(GlobalValidationPluginOptions))]
    [JsonSerializable(typeof(List<GlobalValidationPluginOptions>))]
    internal partial class WacsJson : JsonSerializerContext 
    {
        public static WacsJson Convert(IPluginService _plugin, ILogService _log, ISettingsService _settings)
        {
            var pluginConverter = new Plugin2OptionsConverter(_plugin);
            var options = new JsonSerializerOptions
            { 
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new ProtectedStringConverter(_log, _settings)); 
            options.Converters.Add(new StoresPluginOptionsConverter(pluginConverter));
            options.Converters.Add(pluginConverter);
            return new WacsJson(options);
        }
    }
}
