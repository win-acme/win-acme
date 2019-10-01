using Autofac;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface IPluginService
    {
        ICsrPluginOptionsFactory CsrPluginFactory(ILifetimeScope scope, string name);
        List<ICsrPluginOptionsFactory> CsrPluginOptionsFactories(ILifetimeScope scope);
        List<IInstallationPluginOptionsFactory> InstallationPluginFactories(ILifetimeScope scope);
        IInstallationPluginOptionsFactory InstallationPluginFactory(ILifetimeScope scope, string name);
        List<IArgumentsProvider> OptionProviders();
        List<Type> PluginOptionTypes<T>() where T : PluginOptions;
        List<IStorePluginOptionsFactory> StorePluginFactories(ILifetimeScope scope);
        IStorePluginOptionsFactory StorePluginFactory(ILifetimeScope scope, string name);
        List<ITargetPluginOptionsFactory> TargetPluginFactories(ILifetimeScope scope);
        ITargetPluginOptionsFactory TargetPluginFactory(ILifetimeScope scope, string name);
        List<IValidationPluginOptionsFactory> ValidationPluginFactories(ILifetimeScope scope);
        IValidationPluginOptionsFactory ValidationPluginFactory(ILifetimeScope scope, string type, string name);
    }
}
