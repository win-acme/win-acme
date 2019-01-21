using Autofac;
using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IResolver
    {
        List<IInstallationPluginOptionsFactory> GetInstallationPlugins(ILifetimeScope scope);
        IStorePluginOptionsFactory GetStorePlugin(ILifetimeScope scope);
        ITargetPluginOptionsFactory GetTargetPlugin(ILifetimeScope scope);
        ICsrPluginOptionsFactory GetCsrPlugin(ILifetimeScope scope);
        IValidationPluginOptionsFactory GetValidationPlugin(ILifetimeScope scope, Target target);
    }
}