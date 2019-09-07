using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// TargetPluginFactory base implementation
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class TargetPluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        ITargetPluginOptionsFactory
        where TPlugin : ITargetPlugin
        where TOptions : TargetPluginOptions, new()
    {
        public abstract TOptions Aquire(IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default();
        /// <summary>
        /// Allow implementations to hide themselves from users
        /// in interactive mode
        /// </summary>
        public virtual bool Hidden => false;

        TargetPluginOptions ITargetPluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel) => Aquire(inputService, runLevel);
        TargetPluginOptions ITargetPluginOptionsFactory.Default() => Default();
    }
}
