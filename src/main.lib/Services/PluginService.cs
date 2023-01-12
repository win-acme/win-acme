using ACMESharp.Authorizations;
using Autofac;
using PKISharp.WACS.Plugins;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace PKISharp.WACS.Services
{
    public class PluginService : IPluginService
    {
        private readonly AssemblyService _assemblyService;
        private readonly List<Plugin> _plugins;
        internal readonly ILogService _log;

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

        /// <summary>
        /// Get all plugins
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Plugin> GetPlugins() => _plugins.AsEnumerable();
        
        /// <summary>
        /// Get all plugins for a specific step,
        /// used by the resolvers
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public IEnumerable<Plugin> GetPlugins(Steps step) => GetPlugins().Where(x => x.Step == step);
        
        /// <summary>
        /// Get plugin by Guid
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Plugin? GetPlugin(Guid id) => GetPlugins().Where(x => x.Id == id).OrderByDescending(x => x.Hidden).FirstOrDefault();
        
        /// <summary>
        /// Detect plugin based on provided options,
        /// either by type or by ID
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public Plugin GetPlugin(PluginOptionsBase options) 
        {
            if (TryGetPlugin(options, out var plugin)) 
            {
                return plugin;
            } 
            throw new Exception("Plugin not found");
        }

        /// <summary>
        /// Get the plugin without crashing
        /// </summary>
        /// <param name="options"></param>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public bool TryGetPlugin([NotNullWhen(true)] PluginOptionsBase? options, [NotNullWhen(true)] out Plugin? plugin)
        {
            plugin = default;
            if (options == null)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(options.Plugin))
            {
                // Find plugin based on the type
                plugin = GetPlugins().FirstOrDefault(x => x.Options == options.GetType());
            }
            else if (Guid.TryParse(options.Plugin, out var pluginGuid))
            {
                plugin = GetPlugin(pluginGuid);
            }
            return plugin != null;
        }

        /// <summary>
        /// Get plugin based on command line arguments
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="step"></param>
        /// <param name="name"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public Plugin? GetPlugin(Steps step, string name, string? parameter = null)
        {
            var plugins = GetPlugins(step).
                Where(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)).
                ToList();
            if (step == Steps.Validation)
            {
                var validationCapability = typeof(object);
                switch (parameter?.ToLower())
                {
                    case Http01ChallengeValidationDetails.Http01ChallengeType:
                        validationCapability = typeof(HttpValidationCapability);
                        break;
                    case Dns01ChallengeValidationDetails.Dns01ChallengeType:
                        validationCapability = typeof(HttpValidationCapability);
                        break;
                    case TlsAlpn01ChallengeValidationDetails.TlsAlpn01ChallengeType:
                        validationCapability = typeof(HttpValidationCapability);
                        break;
                }
                plugins = plugins.
                    Where(x => x.Capability.IsAssignableTo(validationCapability)).
                    ToList();
            }
            return plugins.FirstOrDefault();
        }

        /// <summary>
        /// Configure the DI system
        /// </summary>
        /// <param name="builder"></param>
        internal void Configure(ContainerBuilder builder)
        {
            _plugins.ForEach(p =>
            {
                builder.RegisterType(p.Backend).InstancePerLifetimeScope();
                builder.RegisterType(p.OptionsJson).InstancePerLifetimeScope();
                builder.RegisterType(p.OptionsFactory).InstancePerLifetimeScope();
                builder.RegisterType(p.Capability).InstancePerLifetimeScope();
            });
        }

        /// <summary>
        /// Extract meta data from found types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="step"></param>
        private void AddPluginType<T>(Steps step)
        {
            var types = _assemblyService.GetResolvable<T>().ToList();
            ListPlugins(types.Select(x => x.Type), step.ToString().ToLower());
            foreach (var type in types)
            {
                var attributes = type.Type.GetCustomAttributes(true).OfType<IPluginMeta>();
                foreach (var meta in attributes)
                {
                    var existing = _plugins.FirstOrDefault(p => p.Id == meta.Id);
                    if (existing != null && !(existing.Hidden || meta.Hidden))
                    {
                        _log.Warning(
                           "Duplicate plugin with key {key}. " +
                           "{Name1} from {Location1} and " +
                           "{Name2} from {Location2}",
                           meta.Id,
                           type.Type.FullName, type.Type.Assembly.Location,
                           existing.Backend.FullName, existing.Backend.Assembly.Location);
                        continue;
                    }
                    _plugins.Add(new Plugin(type.Type, meta, step));
                }
                if (!attributes.Any()) 
                {
                    _log.Warning("Missing metadata on {type} from {location}", type.Type.FullName, type.Type.Assembly.Location);                  
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
