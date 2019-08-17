using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("5C3DB0FB-840B-469F-B5A7-0635D8E9A93D")]
    class CsrOptions : TargetPluginOptions<Csr>
    {
        public static string NameLabel => "CSR";

        public override string Name => NameLabel;
        public override string Description => "Read a CSR created by another program";
        public string CsrFile { get; set; }
        public string PkFile { get; set; }
    }
}
