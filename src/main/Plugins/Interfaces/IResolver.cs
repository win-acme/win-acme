using Autofac;
using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IResolver
    {
        List<IInstallationPluginOptionsFactory> GetInstallationPlugins(ILifetimeScope scope, IEnumerable<Type> storeType);
        List<IStorePluginOptionsFactory> GetStorePlugins(ILifetimeScope scope);
        ITargetPluginOptionsFactory GetTargetPlugin(ILifetimeScope scope);
        ICsrPluginOptionsFactory GetCsrPlugin(ILifetimeScope scope);
        IValidationPluginOptionsFactory GetValidationPlugin(ILifetimeScope scope, Target target);
    }
}