using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

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
        public TargetPluginOptionsFactory(ILogService log) : base(log) { }

        public abstract TOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(IArgumentsService arguments);
        /// <summary>
        /// Allow implementations to hide themselves from users
        /// in interactive mode
        /// </summary>
        public virtual bool Hidden => false;

        TargetPluginOptions ITargetPluginOptionsFactory.Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(arguments, inputService, runLevel);
        }
        TargetPluginOptions ITargetPluginOptionsFactory.Default(IArgumentsService arguments)
        {
            return Default(arguments);
        }
    }
}
