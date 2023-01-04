using Autofac;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;

namespace PKISharp.WACS.Plugins.Base
{
    internal class PluginHelper
    {
        private readonly ILifetimeScope _scope;
        private readonly IPluginService _pluginService;

        public PluginHelper(ILifetimeScope scope, IPluginService pluginService)
        {
            _pluginService = pluginService;
            _scope = scope;
        }

        /// <summary>
        /// Helper method to construct a backend (execution)
        /// part of the plugin based on previously configured
        /// options
        /// </summary>
        /// <typeparam name="TOptionsFactory"></typeparam>
        /// <typeparam name="TCapability"></typeparam>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public PluginBackend<TBackend, TCapability>
            Backend<TBackend, TCapability>(PluginOptions options)
            where TCapability : IPluginCapability
            where TBackend : IPlugin
        {
            var meta = _pluginService.GetPlugin(options);
            return new PluginBackend<TBackend, TCapability>(
                 meta,
                 Resolve<TBackend>(meta.Backend, new TypedParameter(meta.Options, options)),
                 Resolve<TCapability>(meta.Capability));
        }

        /// <summary>
        /// Helper method to construct a frontend (configuration)
        /// part of the plugin, based plugin metadata found in the 
        /// registry
        /// </summary>
        /// <typeparam name="TOptionsFactory"></typeparam>
        /// <typeparam name="TCapability"></typeparam>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public PluginFrontend<TOptionsFactory, TCapability> 
            Frontend<TOptionsFactory, TCapability>(Plugin plugin)
            where TCapability : IPluginCapability
            where TOptionsFactory : IPluginOptionsFactory
            => new(
                plugin,
                Resolve<TOptionsFactory>(plugin.OptionsFactory),
                Resolve<TCapability>(plugin.Capability));

        /// <summary>
        /// Helper method to resolve any type from the scope
        /// </summary>
        /// <typeparam name="TCast"></typeparam>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private TCast Resolve<TCast>(Type type, params Autofac.Core.Parameter[] parameters)
        {
            var item = _scope.ResolveOptional(type, parameters);
            if (item == null)
            {
                throw new InvalidOperationException($"{type.Name} could not be resolved");
            }
            if (item is not TCast cast)
            {
                throw new InvalidOperationException($"{type.Name} does not implement {typeof(TCast).Name}");
            }
            return cast;
        }
    }
}
