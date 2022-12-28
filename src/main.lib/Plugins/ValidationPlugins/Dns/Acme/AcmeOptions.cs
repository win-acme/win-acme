using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class AcmeOptions : ValidationPluginOptions<Acme>
    {
        public override string Name => "acme-dns";
        public override string Description => "Create verification records with acme-dns (https://github.com/joohoi/acme-dns)";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        public string? BaseUri { get; set; }
    }
}