using Autofac;
using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Plugins
{
    public interface IResolver
    {
        List<IInstallationPluginFactory> GetInstallationPlugins(ILifetimeScope scope);
        IStorePluginFactory GetStorePlugin(ILifetimeScope scope);
        ITargetPluginFactory GetTargetPlugin(ILifetimeScope scope);
        IValidationPluginFactory GetValidationPlugin(ILifetimeScope scope);
    }
}