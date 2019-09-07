using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;

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
        InstallationPluginOptions IInstallationPluginOptionsFactory.Aquire(Target target, IInputService inputService, RunLevel runLevel) => Aquire(target, inputService, runLevel);
        InstallationPluginOptions IInstallationPluginOptionsFactory.Default(Target target) => Default(target);
        public abstract TOptions Aquire(Target target, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(Target target);
        public virtual bool CanInstall(IEnumerable<Type> storeTypes) => true;
    }

}
