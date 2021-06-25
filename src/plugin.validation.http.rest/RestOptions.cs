using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [Plugin("11ba2994-ea59-4f2f-b9eb-0eaa2fa3cbfa")]
    internal sealed class RestOptions : ValidationPluginOptions<Rest>
    {
        public override string Name => "Rest";
        public override string Description => "Send verification files to the server by issuing HTTP REST-style requests";
        public ProtectedString? SecurityToken { get; internal set; }
        public bool? UseHttps { get; internal set; }
    }
}
