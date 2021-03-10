using Autofac;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface IPluginService
    {
        IEnumerable<T> GetFactories<T>(ILifetimeScope scope) where T: IPluginOptionsFactory;
        T? GetFactory<T>(ILifetimeScope scope, string name, string? parameter = null) where T : IPluginOptionsFactory;
        IEnumerable<IArgumentsProvider> ArgumentsProviders();
        IEnumerable<Type> PluginOptionTypes<T>() where T : PluginOptions;
    }
}
