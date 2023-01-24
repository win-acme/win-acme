using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreOptions : StorePluginOptions
    {
        /// <summary>
        /// Name of the certificate store to use
        /// </summary>
        public string? StoreName { get; set; }        
        
        /// <summary>
        /// ACL to add to the private key
        /// </summary>
        public List<string>? AclFullControl { get; set; }

        /// <summary>
        /// Print details to user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            if (!string.IsNullOrEmpty(StoreName))
            {
                input.Show("Name", StoreName, level: 1);
            }
            if (AclFullControl != null)
            {
                input.Show("AclFullControl", string.Join(",", AclFullControl), level: 1);
            }
        }
    }
}
