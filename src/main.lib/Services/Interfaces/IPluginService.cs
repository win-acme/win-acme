using Autofac;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface IPluginService
    {
        List<ITargetPluginOptionsFactory> TargetPluginFactories(ILifetimeScope scope);
        List<IValidationPluginOptionsFactory> ValidationPluginFactories(ILifetimeScope scope);
        List<IOrderPluginOptionsFactory> OrderPluginFactories(ILifetimeScope scope);
        List<ICsrPluginOptionsFactory> CsrPluginOptionsFactories(ILifetimeScope scope);
        List<IStorePluginOptionsFactory> StorePluginFactories(ILifetimeScope scope);
        List<IInstallationPluginOptionsFactory> InstallationPluginFactories(ILifetimeScope scope);

        ITargetPluginOptionsFactory TargetPluginFactory(ILifetimeScope scope, string name);
        IValidationPluginOptionsFactory ValidationPluginFactory(ILifetimeScope scope, string type, string name);
        IOrderPluginOptionsFactory OrderPluginFactory(ILifetimeScope scope, string name);
        ICsrPluginOptionsFactory CsrPluginFactory(ILifetimeScope scope, string name);
        IStorePluginOptionsFactory StorePluginFactory(ILifetimeScope scope, string name);
        IInstallationPluginOptionsFactory InstallationPluginFactory(ILifetimeScope scope, string name);
       
        List<IArgumentsProvider> OptionProviders();
        List<Type> PluginOptionTypes<T>() where T : PluginOptions;
    }
}
