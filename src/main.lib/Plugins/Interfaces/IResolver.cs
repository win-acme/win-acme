using Autofac;
using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IResolver
    {
        Task<Plugin?> GetTargetPlugin(ILifetimeScope scope);

        Task<Plugin?> GetValidationPlugin(ILifetimeScope scope, Target target);
       
        Task<Plugin?> GetOrderPlugin(ILifetimeScope scope, Target target);

        Task<Plugin?> GetCsrPlugin(ILifetimeScope scope);

        Task<Plugin?> GetStorePlugin(ILifetimeScope scope, IEnumerable<Plugin> chosen);

        Task<Plugin?> GetInstallationPlugin(
            ILifetimeScope scope,
            IEnumerable<Plugin> storeType,
            IEnumerable<Plugin> chosen);



    }
}