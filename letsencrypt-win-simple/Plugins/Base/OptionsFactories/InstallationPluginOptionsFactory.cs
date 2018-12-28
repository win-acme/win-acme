using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// InstallationPluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseInstallationPluginFactory<TPlugin, TOptions> :
        BasePluginFactory<TPlugin>,
        IInstallationPluginOptionsFactory
        where TPlugin : IInstallationPlugin
        where TOptions : InstallationPluginOptions, new()
    {
        public BaseInstallationPluginFactory(ILogService log, string name, string description = null) : base(log, name, description) { }
        InstallationPluginOptions IInstallationPluginOptionsFactory.Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(renewal, optionsService, inputService, runLevel);
        }
        InstallationPluginOptions IInstallationPluginOptionsFactory.Default(ScheduledRenewal renewal, IOptionsService optionsService)
        {
            return Default(renewal, optionsService);
        }
        public abstract TOptions Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(ScheduledRenewal renewal, IOptionsService optionsService);
        public virtual bool CanInstall() => true;
    }

}
