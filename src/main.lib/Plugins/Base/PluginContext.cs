using Autofac;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;

namespace PKISharp.WACS.Plugins
{
    /// <summary>
    /// Non-generic version used by resolver
    /// </summary>
    public class PluginFactoryContext : PluginFactoryContext<IPluginOptionsFactory>
    {
        public PluginFactoryContext(Plugin plugin, ILifetimeScope scope) : base(plugin, scope) { }
    }

    /// <summary>
    /// Resolve all components of the store system 
    /// so that it can be reused by the Add, Install and 
    /// Remove stages of the execution
    /// </summary>
    public class PluginFactoryContext<TFactory>
        where TFactory : IPluginOptionsFactory
    {
        public Plugin Meta;
        public TFactory Factory;
        public PluginFactoryContext(Plugin plugin, ILifetimeScope scope)
        {
            Meta = plugin;
            var item = scope.ResolveOptional(Meta.OptionsFactory);
            if (item == null)
            {
                throw new InvalidOperationException($"{Meta.OptionsFactory.Name} could not be resolved");
            }
            if (item is not TFactory factory)
            {
                throw new InvalidOperationException($"{Meta.OptionsFactory.Name} does not implement {typeof(TFactory).Name}");
            }
            Factory = factory;
        }
    }

    /// <summary>
    /// Resolve all components of the store system 
    /// so that it can be reused by the Add, Install and 
    /// Remove stages of the execution
    /// </summary>
    public class PluginExecutionContext<TPlugin, TFactory, TOptions>
        where TOptions : PluginOptions
        where TFactory : IPluginOptionsFactory
        where TPlugin : IPlugin
    {
        public TPlugin Plugin;
        public Plugin Meta;
        public TFactory Factory;
        public TOptions Options;
        public PluginExecutionContext(TOptions options, ILifetimeScope scope)
        {
            var pluginService = scope.Resolve<IPluginService>();
            Options = options;
            Meta = pluginService.GetPlugin(options);
            var item = scope.ResolveOptional(Meta.OptionsFactory);
            if (item == null)
            {
                throw new InvalidOperationException($"{Meta.OptionsFactory.Name} could not be resolved");
            }
            if (item is not TFactory factory)
            {
                throw new InvalidOperationException($"{Meta.OptionsFactory.Name} does not implement {typeof(TFactory).Name}");
            }
            Factory = factory;
            item = scope.ResolveOptional(Meta.Runner, new TypedParameter(Meta.Options, Options));
            if (item == null)
            {
                throw new InvalidOperationException($"{Meta.Runner.Name} could not be resolved");
            }
            if (item is not TPlugin plugin)
            {
                throw new InvalidOperationException($"{Meta.Runner.Name} does not implement {typeof(TPlugin).Name}");
            }
            Plugin = plugin;
        }
    }

    public class StorePluginContext :
        PluginExecutionContext<IStorePlugin, IStorePluginOptionsFactory, StorePluginOptions>
    {
        public StorePluginContext(StorePluginOptions options, ILifetimeScope scope) :
            base(options, scope) { }
    }

    public class InstallationPluginContext :
        PluginExecutionContext<IInstallationPlugin, IInstallationPluginOptionsFactory, InstallationPluginOptions>
    {
        public InstallationPluginContext(InstallationPluginOptions options, ILifetimeScope scope) :
            base(options, scope) { }
    }

    public class ValidationPluginContext :
        PluginExecutionContext<IValidationPlugin, IValidationPluginOptionsFactory, ValidationPluginOptions>
    {
        public ValidationPluginContext(ValidationPluginOptions options, ILifetimeScope scope) : 
            base(options, scope) { }
    }
}
