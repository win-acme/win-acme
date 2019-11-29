using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreArgumentsProvider : BaseArgumentsProvider<CertificateStoreArguments>
    {
        public override string Name => "Certificate Store plugin";
        public override string Group => "Store";
        public override string Condition => "--store certificatestore";
        public override bool Default => true;

        public override void Configure(FluentCommandLineParser<CertificateStoreArguments> parser)
        {
            parser.Setup(o => o.CertificateStore)
                .As("certificatestore")
                .WithDescription("This setting can be used to save the certificate in a specific store. By default it will go to 'WebHosting' store on modern versions of Windows.");
            parser.Setup(o => o.KeepExisting)
                .As("keepexisting")
                .WithDescription("While renewing, do not remove the previous certificate.");
            parser.Setup(o => o.AclFullControl)
                .As("acl-fullcontrol")
                .WithDescription("List of additional principals (besides the owners of the store) that should get full control permissions on the private key of the certificate.");
        }

        public override bool Active(CertificateStoreArguments current) => !string.IsNullOrEmpty(current.CertificateStore) || current.KeepExisting;
    }
}
