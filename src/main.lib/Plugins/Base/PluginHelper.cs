using Autofac;
using PKISharp.WACS.Plugins.Interfaces;
using System;

namespace PKISharp.WACS.Plugins.Base
{
    internal class PluginHelper
    {
        private readonly ILifetimeScope _scope;

        public PluginHelper(ILifetimeScope scope) => _scope = scope;

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
