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
        private readonly List<Plugin> _plugins;
        internal readonly ILogService _log;

        internal void Configure(ContainerBuilder builder)
        {
            _plugins.ForEach(p =>
            {
                builder.RegisterType(p.Runner); 
                builder.RegisterType(p.Meta.OptionsJson);
                builder.RegisterType(p.Meta.OptionsFactory);
            });
        }

        public IEnumerable<Plugin> GetPlugins() => _plugins.AsEnumerable();
        public IEnumerable<Plugin> GetPlugins(Steps step) => GetPlugins().Where(x => x.Step == step);
        public Plugin? GetPlugin(Guid id) => GetPlugins().Where(x => x.Id == id).FirstOrDefault();
        public Plugin? GetPlugin(ILifetimeScope scope, Steps step, string name, string? parameter = null)
        {
            var plugins = GetPlugins(step).
                Select(s => new { plugin = s, factory = scope.Resolve(s.Meta.OptionsFactory) as IPluginOptionsFactory }).
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
                var attributes = type.GetCustomAttributes(true).OfType<IPluginMeta>();
                foreach (var meta in attributes)
                {
                    var existing = _plugins.FirstOrDefault(p => p.Id == meta.Id);
                    if (existing != null)
                    {
                        _log.Warning(
                           "Duplicate plugin with key {key}. " +
                           "{Name1} from {Location1} and " +
                           "{Name2} from {Location2}",
                           meta.Id,
                           type.FullName, type.Assembly.Location,
                           existing.Runner.FullName, existing.Runner.Assembly.Location);
                        continue;
                    }
                    _plugins.Add(new Plugin(type, meta, step));
                }
                if (!attributes.Any()) 
                {
                    _log.Warning("Missing metadata on {type} from {location}", type.FullName, type.Assembly.Location);                  
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
    }
}
