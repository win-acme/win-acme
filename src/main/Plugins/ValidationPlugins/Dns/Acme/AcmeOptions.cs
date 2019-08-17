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
        public override string Description => "Create verification records with acme-dns (https://github.com/joohoi/acme-dns)";
        public override string ChallengeType { get => Constants.Dns01ChallengeType; }

        public string BaseUri { get; set; }
    }
}