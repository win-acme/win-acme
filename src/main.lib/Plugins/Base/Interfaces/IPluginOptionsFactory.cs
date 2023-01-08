using MorseCode.ITask;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IPluginOptionsFactory<out TOptions>
        where TOptions: PluginOptions, new()
    {
        /// <summary>
        /// How its sorted in the menu
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Check or get information needed (interactive)
        /// </summary>
        /// <param name="target"></param>
        ITask<TOptions?> Aquire(IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information needed (unattended)
        /// </summary>
        /// <param name="target"></param>
        ITask<TOptions?> Default();
    }

}
