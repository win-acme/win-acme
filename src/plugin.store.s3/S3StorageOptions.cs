using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [Plugin("63935701-89f6-4169-bd31-fc279d16f6d8")]
    internal class S3StorageOptions : StorePluginOptions<S3Storage>
    {
        public override string Name => "S3";

        public override string Description => "Store certificate in Amazon Storage";

        public string Bucket { get; set; }

        public string FileKey { get; set; }

        public ProtectedString? SecretKey { get; set; }

        public ProtectedString? AccessKey { get; set; }
    }
}
