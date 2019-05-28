using Autofac;
using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IResolver
    {
        IInstallationPluginOptionsFactory GetInstallationPlugin(
            ILifetimeScope scope, 
            IEnumerable<Type> storeType, 
            IEnumerable<IInstallationPluginOptionsFactory> chosen);

        IStorePluginOptionsFactory GetStorePlugin(ILifetimeScope scope, 
            IEnumerable<IStorePluginOptionsFactory> chosen);

        ITargetPluginOptionsFactory GetTargetPlugin(ILifetimeScope scope);

        ICsrPluginOptionsFactory GetCsrPlugin(ILifetimeScope scope);

        IValidationPluginOptionsFactory GetValidationPlugin(ILifetimeScope scope, Target target);
    }
}