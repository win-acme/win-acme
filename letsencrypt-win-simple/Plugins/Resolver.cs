using ACMESharp;
using Autofac;
using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins
{
    class Resolver
    {
        private ScheduledRenewal _renewal;
        private PluginService _plugins;
        private ILogService _log;
        public ILifetimeScope Scope { get; set; }

        public Resolver(ScheduledRenewal renewal, ILogService log, PluginService pluginService)
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
        public ITargetPlugin GetTargetPlugin()
        {
            if (string.IsNullOrWhiteSpace(_renewal.Binding.TargetPluginName))
            {
                switch (_renewal.Binding.PluginName)
                {
                    case AddUpdateIISBindings.PluginName:
                        if (_renewal.Binding.HostIsDns == false)
                        {
                            _renewal.Binding.TargetPluginName = nameof(IISSite);
                        }
                        else
                        {
                            _renewal.Binding.TargetPluginName = nameof(IISBinding);
                        }
                        break;
                    case IISSites.SiteServer:
                        _renewal.Binding.TargetPluginName = nameof(IISSites);
                        break;
                    case RunScript.PluginName:
                        _renewal.Binding.TargetPluginName = nameof(Manual);
                        break;
                }
            }
            return _plugins.GetByName(_plugins.Target, _renewal.Binding.TargetPluginName);
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public IValidationPlugin GetValidationPlugin()
        {
            if (_renewal.Binding.ValidationPluginName == null)
            {
                _renewal.Binding.ValidationPluginName = $"{AcmeProtocol.CHALLENGE_TYPE_HTTP}.{nameof(FileSystem)}";
            }
            var validationPluginFactory = _plugins.GetValidationPlugin(_renewal.Binding.ValidationPluginName);
            if (validationPluginFactory == null)
            {
                _log.Error("Unable to find validation plugin {ValidationPluginName}", _renewal.Binding.ValidationPluginName);
                return null;
            }

            var ret = (IValidationPlugin)Scope.Resolve(validationPluginFactory.GetType(), 
                new TypedParameter(typeof(Target), 
                _renewal.Binding));

            if (ret == null)
            {
                _log.Error("Unable to create validation plugin instance {ValidationPluginName}", _renewal.Binding.ValidationPluginName);
                return null;
            }
            return ret;
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public IInstallationPlugin GetInstallationPlugin()
        {
            if (_renewal.Binding.PluginName == null || _renewal.Binding.PluginName == IISSites.SiteServer)
            {
                _renewal.Binding.PluginName = AddUpdateIISBindings.PluginName;
            }
            var installationPluginFactory = _plugins.GetByName(_plugins.Installation, _renewal.Binding.PluginName);
            if (installationPluginFactory == null)
            {
                _log.Error("Unable to find installation plugin {PluginName}", _renewal.Binding.PluginName);
                return null;
            }

            var ret = (IInstallationPlugin)Scope.Resolve(installationPluginFactory.GetType(),
                new TypedParameter(typeof(Target),
                _renewal.Binding));

            if (ret == null)
            {
                _log.Error("Unable to create installation plugin instance {PluginName}", _renewal.Binding.PluginName);
                return null;
            }
            return ret;
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public IStorePlugin GetStorePlugin()
        {
            return _renewal.CentralSsl ?
                (IStorePlugin)(new CentralSsl(_renewal, _log)) :
                new CertificateStore(_renewal, _log);
        }
    }
}
