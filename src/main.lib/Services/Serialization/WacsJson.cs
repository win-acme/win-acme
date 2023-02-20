using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.SecretPlugins;
using PKISharp.WACS.Services.Legacy;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using static PKISharp.WACS.Clients.Acme.ZeroSsl;
using static PKISharp.WACS.Clients.UpdateClient;
using static PKISharp.WACS.Services.ValidationOptionsService;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Code generator for built-in types
    /// </summary>
    [JsonSerializable(typeof(PluginOptionsBase))]
    [JsonSerializable(typeof(CsrPluginOptions))]
    [JsonSerializable(typeof(Renewal))]
    [JsonSerializable(typeof(VersionCheckData))]
    [JsonSerializable(typeof(GlobalValidationPluginOptions))]
    [JsonSerializable(typeof(List<GlobalValidationPluginOptions>))]
    [JsonSerializable(typeof(ZeroSslEabCredential))]
    [JsonSerializable(typeof(LegacyScheduledRenewal))]
    [JsonSerializable(typeof(List<JsonSecretService.CredentialEntry>))]
    internal partial class WacsJson : JsonSerializerContext 
    {
        public WacsJson(WacsJsonOptionsFactory optionsFactory) : base(optionsFactory.Options) {}
        public static WacsJson Insensitive => new(new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        public static void Configure(ContainerBuilder builder)
        {
            _ = builder.RegisterType<PluginOptionsConverter>().SingleInstance();
            _ = builder.RegisterType<PluginOptionsListConverter>().SingleInstance();
            _ = builder.RegisterType<WacsJson>().SingleInstance();
            _ = builder.RegisterType<WacsJsonOptionsFactory>().SingleInstance();
            _ = builder.RegisterType<WacsJsonPluginsOptionsFactory>().SingleInstance();
            _ = builder.RegisterType<WacsJsonPlugins>().SingleInstance();
        }
    }
}
