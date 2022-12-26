using Autofac;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Services
{
    public class PluginService : IPluginService
    {
        private readonly AssemblyService _assemblyService;
        private readonly List<Type> _optionFactories;
        private readonly List<Type> _plugins;
        internal readonly ILogService _log;

        public IEnumerable<Type> PluginOptionTypes<T>() where T : PluginOptions => _assemblyService.GetResolvable<T>();

        internal void Configure(ContainerBuilder builder)
        {
            _optionFactories.ForEach(t => builder.RegisterType(t).SingleInstance());
            _plugins.ForEach(ip => builder.RegisterType(ip));
        }

        public IEnumerable<T> GetFactories<T>(ILifetimeScope scope) where T : IPluginOptionsFactory => _optionFactories.Select(scope.Resolve).OfType<T>().OrderBy(x => x.Order).ToList();

        private IEnumerable<T> GetByName<T>(string name, ILifetimeScope scope) where T : IPluginOptionsFactory => GetFactories<T>(scope).Where(x => x.Match(name));

        public PluginService(ILogService logger, AssemblyService assemblyService)
        {
            _log = logger;
            _assemblyService = assemblyService;
            _optionFactories = _assemblyService.GetResolvable<IPluginOptionsFactory>(true);
            _plugins = new List<Type>();
            AddPluginType<ITargetPlugin>("target");
            AddPluginType<IValidationPlugin>("validation");
            AddPluginType<IOrderPlugin>("order");
            AddPluginType<ICsrPlugin>("csr");
            AddPluginType<IStorePlugin>("store");
            AddPluginType<IInstallationPlugin>("installation");
        }

        private void AddPluginType<T>(string name)
        {
            var temp = _assemblyService.GetResolvable<T>();
            ListPlugins(temp, name);
            _plugins.AddRange(temp);
        }

        /// <summary>
        /// Log externally loaded plugins to the screen in verbose mode
        /// </summary>
        /// <param name="list"></param>
        /// <param name="type"></param>
        private void ListPlugins(IEnumerable<Type> list, string type)
        {
            _ = list.Where(x => x.Assembly != typeof(PluginService).Assembly).
                All(x =>
                {
                    _log.Verbose("Loaded {type} plugin {name} from {location}", type, x.Name, x.Assembly.Location);
                    return false;
                });
        }

        /// <summary>
        /// Find chosen plugin options factory based on command line parameter(s)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scope"></param>
        /// <param name="name"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public T? GetFactory<T>(ILifetimeScope scope, string name, string? parameter = null) where T : IPluginOptionsFactory
        {
            var plugins = GetByName<T>(name, scope);
            if (typeof(T) == typeof(IValidationPluginOptionsFactory) && plugins.Count() > 1)
            {
                plugins = plugins.Where(x => string.Equals(parameter, (x as IValidationPluginOptionsFactory)?.ChallengeType, StringComparison.InvariantCultureIgnoreCase));
            }
            return plugins.FirstOrDefault();
        }
    }
}
