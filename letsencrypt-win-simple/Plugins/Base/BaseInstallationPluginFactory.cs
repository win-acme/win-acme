using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.Base
{
    /// <summary>
    /// StorePluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class BaseInstallationPluginFactory<T> : BasePluginFactory<T>, IInstallationPluginFactory where T : IInstallationPlugin
    {
        public BaseInstallationPluginFactory(ILogService log, string name, string description = null) : base(log, name, description) { }
        public virtual void Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService) { }
        public virtual bool CanInstall(ScheduledRenewal renewal) => true;
        public virtual void Default(ScheduledRenewal renewal, IOptionsService optionsService) { }
    }

}
