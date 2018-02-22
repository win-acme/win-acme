using Autofac;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IResolver
    {
        List<IInstallationPluginFactory> GetInstallationPlugins(ILifetimeScope scope);
        IStorePluginFactory GetStorePlugin(ILifetimeScope scope);
        ITargetPluginFactory GetTargetPlugin(ILifetimeScope scope);
        IValidationPluginFactory GetValidationPlugin(ILifetimeScope scope);
    }
}