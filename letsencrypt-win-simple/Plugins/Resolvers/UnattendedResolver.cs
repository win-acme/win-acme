using ACMESharp;
using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http;
using LetsEncrypt.ACME.Simple.Services;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Plugins
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
        public virtual ITargetPluginFactory GetTargetPlugin()
        {
            // Backwards compatibility
            if (string.IsNullOrWhiteSpace(_renewal.Binding.TargetPluginName))
            {
                switch (_renewal.Binding.PluginName)
                {
                    case IISInstallerFactory.PluginName:
                        if (_renewal.Binding.HostIsDns == false)
                        {
                            _renewal.Binding.TargetPluginName = nameof(IISSite);
                        }
                        else
                        {
                            _renewal.Binding.TargetPluginName = nameof(IISBinding);
                        }
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
            var targetPluginFactory = _plugins.GetByName(_plugins.Target, _renewal.Binding.TargetPluginName);
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
        public virtual IValidationPluginFactory GetValidationPlugin()
        {
            // Backwards compatibility
            if (_renewal.Binding.ValidationPluginName == null)
            {
                _renewal.Binding.ValidationPluginName = $"{AcmeProtocol.CHALLENGE_TYPE_HTTP}.{nameof(FileSystem)}";
            }

            // Get plugin factory
            var validationPluginFactory = _plugins.GetValidationPlugin(_renewal.Binding.ValidationPluginName);
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
        public virtual List<IInstallationPluginFactory> GetInstallationPlugins()
        {
            // Backwards compatibility
            if (_renewal.InstallationPluginNames == null)
            {
                _renewal.InstallationPluginNames = new List<string>();

                // Based on legacy property
                if (_renewal.Binding.PluginName == IISSitesFactory.SiteServer ||
                    _renewal.Binding.PluginName == IISInstallerFactory.PluginName)
                {
                    _renewal.InstallationPluginNames.Add(IISInstallerFactory.PluginName);
                }
                else if (_renewal.Binding.TargetPluginName == nameof(IISSite) ||
                    _renewal.Binding.TargetPluginName == nameof(IISSites) ||
                    _renewal.Binding.TargetPluginName == nameof(IISBinding))
                {
                    _renewal.InstallationPluginNames.Add(IISInstallerFactory.PluginName);
                }
                
                // Based on command line
                if (!string.IsNullOrEmpty(_renewal.Script))
                {
                    _renewal.InstallationPluginNames.Add(ScriptInstallerFactory.PluginName);
                }
                else if (_renewal.Binding.TargetPluginName == nameof(Manual))
                {
                    _renewal.InstallationPluginNames.Add(ScriptInstallerFactory.PluginName);
                }
            }

            // Get plugin factory
            var ret = new List<IInstallationPluginFactory>();
            foreach (var name in _renewal.InstallationPluginNames)
            {
                var installationPluginFactory = _plugins.GetByName(_plugins.Installation, name);
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
        public virtual IStorePluginFactory GetStorePlugin()
        {
            return _renewal.CentralSsl ? 
                _plugins.GetByName(_plugins.Store, nameof(CentralSsl)) :
                _plugins.GetByName(_plugins.Store, nameof(CertificateStore));
        }
    }
}
