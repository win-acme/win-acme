using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.Base.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    [JsonSerializable(typeof(Validation.Dns.ScriptOptions))]
    [JsonSerializable(typeof(Validation.Http.FileSystemOptions))]
    [JsonSerializable(typeof(Validation.Http.FtpOptions))]
    [JsonSerializable(typeof(Validation.Http.SelfHostingOptions))]
    [JsonSerializable(typeof(Validation.Http.SftpOptions))]
    [JsonSerializable(typeof(Validation.Http.WebDavOptions))]
    [JsonSerializable(typeof(Order.DomainOptions))]
    [JsonSerializable(typeof(Order.HostOptions))]
    [JsonSerializable(typeof(Order.SingleOptions))]
    [JsonSerializable(typeof(Csr.EcOptions))]
    [JsonSerializable(typeof(Csr.RsaOptions))]
    [JsonSerializable(typeof(Installation.IISFtpOptions))]
    [JsonSerializable(typeof(NullInstallationOptions))]
    [JsonSerializable(typeof(NullStoreOptions))]
    [JsonSerializable(typeof(NullInstallationOptions))]
    [JsonSerializable(typeof(NullInstallationOptions))]
    internal partial class WacsJson : JsonSerializerContext 
    {
        public static WacsJson Convert(IPluginService _plugin, ILogService _log, ISettingsService _settings)
        {
            var storeConverter = new PluginOptionsConverter<StorePluginOptions>(_plugin.PluginOptionTypes<StorePluginOptions>(), _log);
            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;
            options.Converters.Add(new ProtectedStringConverter(_log, _settings)); 
            options.Converters.Add(new StoresPluginOptionsConverter(storeConverter));
            options.Converters.Add(new PluginOptionsConverter<CsrPluginOptions>(_plugin.PluginOptionTypes<CsrPluginOptions>(), _log));
            options.Converters.Add(new PluginOptionsConverter<OrderPluginOptions>(_plugin.PluginOptionTypes<OrderPluginOptions>(), _log));
            options.Converters.Add(new PluginOptionsConverter<ValidationPluginOptions>(_plugin.PluginOptionTypes<ValidationPluginOptions>(), _log));
            options.Converters.Add(new PluginOptionsConverter<InstallationPluginOptions>(_plugin.PluginOptionTypes<InstallationPluginOptions>(), _log));
            options.Converters.Add(new Plugin2OptionsConverter(_plugin));
            return new WacsJson(options);
        }
    }

    /// <summary>
    /// Code generator for built-in types (duplicate class names)
    /// </summary>
    [JsonSerializable(typeof(Validation.Dns.ManualOptions))]
    [JsonSerializable(typeof(Validation.Tls.SelfHostingOptions))]
    [JsonSerializable(typeof(Installation.ScriptOptions))]
    [JsonSerializable(typeof(Installation.IISOptions))]
    internal partial class WacsJson2 {}
}
