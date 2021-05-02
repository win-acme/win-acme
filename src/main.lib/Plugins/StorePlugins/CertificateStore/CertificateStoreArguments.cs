using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreArguments : BaseArguments
    {
        public override string Name => "Certificate Store plugin";
        public override string Group => "Store";
        public override string Condition => "--store certificatestore";
        public override bool Default => true;

        [CommandLine(Description = "This setting can be used to save the certificate in a specific store. By default it will go to 'WebHosting' store on modern versions of Windows.")]
        public string? CertificateStore { get; set; }

        [CommandLine(Description = "While renewing, do not remove the previous certificate.")]
        public bool KeepExisting { get; set; }

        [CommandLine(Name = "acl-fullcontrol", Description = "List of additional principals (besides the owners of the store) that should get full control permissions on the private key of the certificate.")]
        public string? AclFullControl { get; set; }
    }
}
