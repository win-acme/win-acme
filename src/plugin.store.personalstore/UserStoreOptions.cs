using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [JsonSerializable(typeof(UserStoreOptions))]
    internal partial class UserStoreJson : JsonSerializerContext
    {
        public UserStoreJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class UserStoreOptions : StorePluginOptions { }
}
