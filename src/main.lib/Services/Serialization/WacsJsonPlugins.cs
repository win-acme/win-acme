using System.Text.Json.Serialization;
using Csr = PKISharp.WACS.Plugins.CsrPlugins;
using Store = PKISharp.WACS.Plugins.StorePlugins;
using Installation = PKISharp.WACS.Plugins.InstallationPlugins;
using Order = PKISharp.WACS.Plugins.OrderPlugins;
using Target = PKISharp.WACS.Plugins.TargetPlugins;
using Validation = PKISharp.WACS.Plugins.ValidationPlugins;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Code generator all built-in PluginOptions implementations
    /// </summary>
    [JsonSerializable(typeof(Target.ManualOptions))]
    [JsonSerializable(typeof(Target.IISOptions))]
    [JsonSerializable(typeof(Target.IISBindingOptions))]
    [JsonSerializable(typeof(Target.IISSiteOptions))]
    [JsonSerializable(typeof(Target.IISSitesOptions))]
    [JsonSerializable(typeof(Target.CsrOptions))]
    [JsonSerializable(typeof(Validation.Dns.AcmeOptions))]
    [JsonSerializable(typeof(Validation.Dns.ManualOptions), TypeInfoPropertyName = "DnsManualOptions")]
    [JsonSerializable(typeof(Validation.Dns.ScriptOptions))]
    [JsonSerializable(typeof(Validation.Http.FileSystemOptions))]
    [JsonSerializable(typeof(Validation.Http.SelfHostingOptions))]
    [JsonSerializable(typeof(Validation.Http.WebDavOptions))]
    [JsonSerializable(typeof(Validation.Tls.SelfHostingOptions), TypeInfoPropertyName = "TlsSelfHostingOptions")]
    [JsonSerializable(typeof(Order.DomainOptions))]
    [JsonSerializable(typeof(Order.HostOptions))]
    [JsonSerializable(typeof(Order.SingleOptions))]
    [JsonSerializable(typeof(Order.SiteOptions))]
    [JsonSerializable(typeof(Csr.EcOptions))]
    [JsonSerializable(typeof(Csr.RsaOptions))]
    [JsonSerializable(typeof(Store.CentralSslOptions))]
    [JsonSerializable(typeof(Store.CertificateStoreOptions))]
    [JsonSerializable(typeof(Store.PemFilesOptions))]
    [JsonSerializable(typeof(Store.PfxFileOptions))]
    [JsonSerializable(typeof(Installation.IISFtpOptions))]
    [JsonSerializable(typeof(Installation.IISOptions), TypeInfoPropertyName = "InstallationIISOptions")]
    [JsonSerializable(typeof(Installation.ScriptOptions), TypeInfoPropertyName = "InstallationScriptOptions")]
    [JsonSerializable(typeof(Installation.NullOptions), TypeInfoPropertyName = "InstallationNullOptions")]
    [JsonSerializable(typeof(Store.NullOptions), TypeInfoPropertyName = "StoreNullOptions")]
    internal partial class WacsJsonPlugins : JsonSerializerContext
    {
        public WacsJsonPlugins(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }
}
