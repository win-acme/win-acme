using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("c13acc1b-7571-432b-9652-7a68a5f506c5")]
    class AcmeOptions : ValidationPluginOptions<Acme>
    {
        public override string Name => "acme-dns";
        public override string Description => "CNAME the record to a server that supports the acme-dns API";
        public override string ChallengeType { get => Constants.Dns01ChallengeType; }

        public string BaseUri { get; set; }
        public string UserName { get; set; }
        public string PasswordSafe { get; set; }
        public string Subdomain { get; set; }

        [JsonIgnore]
        public string Password
        {
            get => PasswordSafe.Unprotect();
            set => PasswordSafe = value.Protect();
        }

    }
}