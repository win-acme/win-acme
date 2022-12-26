using Autofac;
using PKISharp.WACS.Plugins.Base;
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
        private readonly List<Type> _pluginsRunners;
        private readonly List<Plugin> _plugins;
        internal readonly ILogService _log;

        public IEnumerable<Type> PluginOptionTypes<T>() where T : PluginOptions => _assemblyService.GetResolvable<T>();

        internal void Configure(ContainerBuilder builder)
        {
            _optionFactories.ForEach(t => builder.RegisterType(t).SingleInstance());
            _pluginsRunners.ForEach(ip => builder.RegisterType(ip));
            _plugins.ForEach(p =>
            {
                builder.RegisterType(p.Runner);
                builder.RegisterType(p.Factory);
            });
        }

        private IEnumerable<T> GetByName<T>(string name, ILifetimeScope scope) where T : IPluginOptionsFactory => GetFactories<T>(scope).Where(x => x.Match(name));
        public IEnumerable<T> GetFactories<T>(ILifetimeScope scope) where T : IPluginOptionsFactory => _optionFactories.Select(scope.Resolve).OfType<T>().OrderBy(x => x.Order).ToList();
       
        public IEnumerable<Plugin> GetPlugins(Steps step) => _plugins.Where(x => x.Step == step);
        public Plugin? GetPlugin(ILifetimeScope scope, Steps step, string name, string? parameter = null)
        {
            var plugins = GetPlugins(step).
                Select(s => new { plugin = s, factory = scope.Resolve(s.Factory) as IPluginOptionsFactory }).
                Where(s => s.factory?.Match(name) ?? false).
                ToList();
            if (step == Steps.Validation && plugins.Count > 1)
            {
                plugins = plugins.
                    Where(x =>
                    {
                        var challengeType = (x.factory as IValidationPluginOptionsFactory)?.ChallengeType;
                        return string.Equals(challengeType, parameter, StringComparison.InvariantCultureIgnoreCase);
                    }).
                    ToList();
            }
            return plugins.FirstOrDefault()?.plugin;
        }

        public PluginService(ILogService logger, AssemblyService assemblyService)
        {
            _log = logger;
            _assemblyService = assemblyService;
            _optionFactories = _assemblyService.GetResolvable<IPluginOptionsFactory>(true).Where(x => x is not ITargetPluginOptionsFactory).ToList();
            _pluginsRunners = new List<Type>();
            _plugins = new List<Plugin>();
            AddPluginType<ITargetPlugin>(Steps.Target);
            AddPluginType<IValidationPlugin>(Steps.Validation);
            AddPluginType<IOrderPlugin>(Steps.Order);
            AddPluginType<ICsrPlugin>(Steps.Csr);
            AddPluginType<IStorePlugin>(Steps.Store);
            AddPluginType<IInstallationPlugin>(Steps.Installation);
        }

        private void AddPluginType<T>(Steps step)
        {
            var types = _assemblyService.GetResolvable<T>();
            ListPlugins(types, step.ToString().ToLower());
            foreach (var type in types)
            {
                var meta = type.GetCustomAttributes(true).OfType<Plugin2Attribute>().FirstOrDefault();
                if (meta != null)
                {
                    _plugins.Add(new Plugin(type, meta, step));
                } 
                else
                {
                    _pluginsRunners.Add(type);
                }
            }
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
