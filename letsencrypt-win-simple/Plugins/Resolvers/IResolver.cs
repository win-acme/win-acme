using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;

namespace LetsEncrypt.ACME.Simple.Plugins
{
    public interface IResolver
    {
        IInstallationPluginFactory GetInstallationPlugin();
        IStorePluginFactory GetStorePlugin();
        ITargetPluginFactory GetTargetPlugin();
        IValidationPluginFactory GetValidationPlugin();
    }
}