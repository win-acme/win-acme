using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base
{
    /// <summary>
    /// StorePluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseInstallationPluginFactory<T> : BasePluginFactory<T>, IInstallationPluginFactory where T : IInstallationPlugin
    {
        public BaseInstallationPluginFactory(ILogService log, string name, string description = null) : base(log, name, description) { }
        public virtual void Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel) { }
        public virtual bool CanInstall(ScheduledRenewal renewal) => true;
        public virtual void Default(ScheduledRenewal renewal, IOptionsService optionsService) { }
    }

}
