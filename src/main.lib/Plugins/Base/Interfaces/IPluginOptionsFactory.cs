using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IPluginOptionsFactory
    {
        /// <summary>
        /// How its sorted in the menu
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Check or get information needed (interactive)
        /// </summary>
        /// <param name="target"></param>
        Task<PluginOptions?> Aquire(IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information needed (unattended)
        /// </summary>
        /// <param name="target"></param>
        Task<PluginOptions?> Default();
    }

}
