using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// TargetPluginFactory interface
    /// </summary>
    public interface ITargetPluginOptionsFactory : IPluginOptionsFactory<TargetPluginOptions>
    {
        /// <summary>
        /// Hide when it cannot be chosen
        /// </summary>
        bool Hidden { get; }
    }
}
