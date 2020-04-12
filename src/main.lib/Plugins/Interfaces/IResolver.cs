using Autofac;
using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IResolver
    {
        Task<ITargetPluginOptionsFactory> GetTargetPlugin(ILifetimeScope scope);

        Task<IValidationPluginOptionsFactory> GetValidationPlugin(ILifetimeScope scope, Target target);
       
        Task<IOrderPluginOptionsFactory> GetOrderPlugin(ILifetimeScope scope);

        Task<ICsrPluginOptionsFactory> GetCsrPlugin(ILifetimeScope scope);

        Task<IStorePluginOptionsFactory?> GetStorePlugin(ILifetimeScope scope, IEnumerable<IStorePluginOptionsFactory> chosen);

        Task<IInstallationPluginOptionsFactory?> GetInstallationPlugin(
            ILifetimeScope scope,
            IEnumerable<Type> storeType,
            IEnumerable<IInstallationPluginOptionsFactory> chosen);



    }
}