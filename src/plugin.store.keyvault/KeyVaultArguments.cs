using PKISharp.WACS.Plugins.Azure.Common;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class KeyVaultArguments : AzureArgumentsCommon
    {
        public string VaultName { get; set; }
        public string CertificateName { get; set; }
    }
}