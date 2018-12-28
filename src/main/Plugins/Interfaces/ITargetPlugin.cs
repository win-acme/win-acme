using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface ITargetPlugin
    {
        /// <summary>
        /// Generate target based on the specified options
        /// </summary>
        /// <returns></returns>
        Target Generate(TargetPluginOptions options);
    }
}
