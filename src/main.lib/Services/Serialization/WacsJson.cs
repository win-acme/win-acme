using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using static PKISharp.WACS.Services.ValidationOptionsService;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Code generator for built-in types
    /// </summary>
    [JsonSerializable(typeof(PluginOptionsBase))]
    [JsonSerializable(typeof(CsrPluginOptions))]
    [JsonSerializable(typeof(Renewal))]
    [JsonSerializable(typeof(GlobalValidationPluginOptions))]
    [JsonSerializable(typeof(List<GlobalValidationPluginOptions>))]
    internal partial class WacsJson : JsonSerializerContext 
    {
        public WacsJson(WacsJsonOptionsFactory optionsFactory) : base(optionsFactory.Options) {}
    }
}
