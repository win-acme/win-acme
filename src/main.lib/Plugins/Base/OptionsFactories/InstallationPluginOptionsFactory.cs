using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// InstallationPluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class InstallationPluginFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        IInstallationPluginOptionsFactory
        where TPlugin : IInstallationPlugin
        where TOptions : InstallationPluginOptions, new()
    {
        async Task<InstallationPluginOptions> IInstallationPluginOptionsFactory.Aquire(Target target, IInputService inputService, RunLevel runLevel) => await Aquire(target, inputService, runLevel);
        async Task<InstallationPluginOptions> IInstallationPluginOptionsFactory.Default(Target target) => await Default(target);
        public abstract Task<TOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel);
        public abstract Task<TOptions> Default(Target target);
        public virtual bool CanInstall(IEnumerable<Type> storeTypes) => true;
    }

}
