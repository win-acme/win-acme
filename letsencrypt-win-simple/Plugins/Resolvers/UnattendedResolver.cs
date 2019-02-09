using ACMESharp;
using Autofac;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Resolvers
{
    public class UnattendedResolver : IResolver
    {
        private ScheduledRenewal _renewal;
        private PluginService _plugins;
        private ILogService _log;

        public UnattendedResolver(ScheduledRenewal renewal, ILogService log, PluginService pluginService)
        {
            _renewal = renewal;
            _log = log;
            _plugins = pluginService;
        }

        /// <summary>
        /// Get the TargetPlugin which was used (or can be assumed to have been used) to create this
        /// ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual ITargetPluginFactory GetTargetPlugin(ILifetimeScope scope)
        {
            // Backwards compatibility
            if (string.IsNullOrWhiteSpace(_renewal.Binding.TargetPluginName))
            {
                switch (_renewal.Binding.PluginName)
                {
                    case IISWebInstallerFactory.PluginName:
                        _renewal.Binding.TargetPluginName = _renewal.Binding.HostIsDns == false ? nameof(IISSite) : nameof(IISBinding);
                        break;
                    case IISSitesFactory.SiteServer:
                        _renewal.Binding.TargetPluginName = nameof(IISSites);
                        break;
                    case ScriptInstallerFactory.PluginName:
                        _renewal.Binding.TargetPluginName = nameof(Manual);
                        break;
                }
            }

            // Get plugin factory
            var targetPluginFactory = _plugins.TargetPluginFactory(scope, _renewal.Binding.TargetPluginName);
            if (targetPluginFactory == null)
            {
                _log.Error("Unable to find target plugin {PluginName}", _renewal.Binding.TargetPluginName);
                return new NullTargetFactory(); 
            }
            return targetPluginFactory;
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual IValidationPluginFactory GetValidationPlugin(ILifetimeScope scope)
        {
            // Backwards compatibility
            if (_renewal.Binding.ValidationPluginName == null)
            {
                _renewal.Binding.ValidationPluginName = $"{AcmeProtocol.CHALLENGE_TYPE_HTTP}.{nameof(FileSystem)}";
            }

            // Get plugin factory
            var validationPluginFactory = _plugins.ValidationPluginFactory(scope, _renewal.Binding.ValidationPluginName);
            if (validationPluginFactory == null)
            {
                _log.Error("Unable to find validation plugin {PluginName}", _renewal.Binding.ValidationPluginName);
                return new NullValidationFactory();
            }
            return validationPluginFactory;
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual List<IInstallationPluginFactory> GetInstallationPlugins(ILifetimeScope scope)
        {
            // Backwards compatibility
            if (_renewal.InstallationPluginNames == null)
            {
                _renewal.InstallationPluginNames = new List<string>();

                // Based on legacy property
                if (_renewal.Binding.PluginName == IISSitesFactory.SiteServer ||
                    _renewal.Binding.PluginName == IISWebInstallerFactory.PluginName)
                {
                    _renewal.InstallationPluginNames.Add(IISWebInstallerFactory.PluginName);
                }
                else if (_renewal.Binding.TargetPluginName == nameof(IISSite) ||
                    _renewal.Binding.TargetPluginName == nameof(IISSites) ||
                    _renewal.Binding.TargetPluginName == nameof(IISBinding))
                {
                    _renewal.InstallationPluginNames.Add(IISWebInstallerFactory.PluginName);
                }
                
                // Based on command line
                if (!string.IsNullOrEmpty(_renewal.Script) || !string.IsNullOrEmpty(_renewal.ScriptParameters))
                {
                    _renewal.InstallationPluginNames.Add(ScriptInstallerFactory.PluginName);
                }

                // Cannot find anything, then it's no installation steps
                if (_renewal.InstallationPluginNames.Count == 0)
                {
                    _renewal.InstallationPluginNames.Add(NullInstallationFactory.PluginName);
                }
            }

            // Get plugin factory
            var ret = new List<IInstallationPluginFactory>();
            foreach (var name in _renewal.InstallationPluginNames)
            {
                var installationPluginFactory = _plugins.InstallationPluginFactory(scope, name);
                if (installationPluginFactory == null)
                {
                    _log.Error("Unable to find installation plugin {PluginName}", name);
                }
                else
                {
                    ret.Add(installationPluginFactory);
                }
            }
            return ret;
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public virtual IStorePluginFactory GetStorePlugin(ILifetimeScope scope)
        {
            return _renewal.CentralSsl ? 
                _plugins.StorePluginFactory(scope, nameof(CentralSsl)) :
                _plugins.StorePluginFactory(scope, nameof(CertificateStore));
        }
    }
}
