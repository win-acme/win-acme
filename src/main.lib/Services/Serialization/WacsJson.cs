using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
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
    internal partial class WacsJson : JsonSerializerContext 
    {
        public WacsJson(WacsJsonOptionsFactory optionsFactory) : base(optionsFactory.Options) {}
    
        public static void Configure(ContainerBuilder builder)
        {
            _ = builder.Register(x =>
            {
                var context = x.Resolve<IComponentContext>();
                if (context is ILifetimeScope scope)
                {
                    return new PluginOptionsConverter(scope);
                }
                throw new Exception();
            }).As<PluginOptionsConverter>().SingleInstance();
            _ = builder.RegisterType<PluginOptionsListConverter>().SingleInstance();
            _ = builder.RegisterType<WacsJson>().SingleInstance();
            _ = builder.RegisterType<WacsJsonOptionsFactory>().SingleInstance();
            _ = builder.RegisterType<WacsJsonPluginsOptionsFactory>().SingleInstance();
            _ = builder.RegisterType<WacsJsonPlugins>().SingleInstance();
        }
    }
}
