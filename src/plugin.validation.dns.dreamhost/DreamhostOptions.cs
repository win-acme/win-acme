using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    class DreamhostOptions : ValidationPluginOptions<DreamhostDnsValidation>
    {
        public override string Name => "Dreamhost";

        public override string Description => "Change records in Dreamhost DNS";

        public override string ChallengeType => Constants.Dns01ChallengeType;

        public string SecretSafe { get; set; }

        [JsonIgnore]
        public string ApiKey
        {
            get => SecretSafe.Unprotect();
            set => SecretSafe = value.Protect();
        }
    }
}
