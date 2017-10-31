using Autofac;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple
{
    public class ScheduledRenewal
    {
        /// <summary>
        /// Reference to the logger
        /// </summary>
        private ILogService _log;

        /// <summary>
        /// Constructor
        /// </summary>
        public ScheduledRenewal()
        {
            _log = Program.Container.Resolve<ILogService>();
        }

        /// <summary>
        /// Has this renewal been saved?
        /// </summary>
        [JsonIgnore]
        internal bool New { get; set; }

        /// <summary>
        /// Has this renewal been changed?
        /// </summary>
        [JsonIgnore]
        internal bool Updated { get; set; }

        /// <summary>
        /// Next scheduled renew date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Information about the certificate
        /// </summary>
        public Target Binding { get; set; }

        /// <summary>
        /// Location of the Central SSL store (if not specified, certificate
        /// is stored in the Certificate store instead.
        /// </summary>
        [JsonProperty(PropertyName = "CentralSsl")]
        public string CentralSslStore { get; set; }

        /// <summary>
        /// Shortcut
        /// </summary>
        [JsonIgnore]
        internal bool CentralSsl
        {
            get
            {
                return !string.IsNullOrWhiteSpace(CentralSslStore);
            }
        }

        /// <summary>
        /// Legacy, replaced by HostIsDns parameter on Target
        /// </summary>
        [Obsolete]
        public bool? San { get; set; }

        /// <summary>
        /// Do not delete previously issued certificate
        /// </summary>
        public bool KeepExisting { get; set; }

        /// <summary>
        /// Script to run on succesful renewal
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Parameters for script
        /// </summary>
        public string ScriptParameters { get; set; }

        /// <summary>
        /// Warmup target website (applies to http-01 validation)
        /// TODO: remove
        /// </summary>
        public bool Warmup { get; set; }

        /// <summary>
        /// History for this renewal
        /// </summary>
        [JsonIgnore]
        public List<RenewResult> History { get; set; }

        /// <summary>
        /// Pretty format
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Binding?.Host ?? "[unknown]"} - renew after {Date.ToUserString()}";

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public IInstallationPlugin GetInstallationPlugin()
        {
            if (Binding.PluginName == null)
            {
                Binding.PluginName = AddUpdateIISBindings.PluginName;
            }
            var installationPluginBase = Program.Plugins.GetByName(Program.Plugins.Installation, Binding.PluginName);
            if (installationPluginBase == null)
            {
                _log.Error("Unable to find installation plugin {PluginName}", Binding.PluginName);
                return null;
            }
            var ret = installationPluginBase.CreateInstance(this);
            if (ret == null)
            {
                _log.Error("Unable to create installation plugin instance {PluginName}", Binding.PluginName);
                return null;
            }
            return ret;
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        IStorePlugin GetStorePlugin()
        {
            if (_StorePlugin == null)
            {
                _StorePlugin = CentralSsl ? 
                    (IStorePlugin)(new CentralSsl(this, _log)) : 
                    new CertificateStore(this, _log);
            }
            return _StorePlugin;
        }
        private IStorePlugin _StorePlugin;

        /// <summary>
        /// Save new certificate to the store that this renewal
        /// is configured to use
        /// </summary>
        /// <param name="certificateInfo"></param>
        public void SaveCertificate(CertificateInfo certificateInfo)
        {
            GetStorePlugin().Save(certificateInfo);
        }

        /// <summary>
        /// Delete certificate from the store that this renewal
        /// is configured to use
        /// </summary>
        /// <param name="certificateInfo"></param>
        public void DeleteCertificate(CertificateInfo certificateInfo)
        {
            GetStorePlugin().Delete(certificateInfo);
        }

        /// <summary>
        /// Find the most recently issued certificate for a specific target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public CertificateInfo Certificate()
        {
            var thumbprint = History?.
                OrderByDescending(x => x.Date).
                Where(x => x.Success).
                Select(x => x.Thumbprint).
                FirstOrDefault();
            var useThumbprint = !string.IsNullOrEmpty(thumbprint);
            var storePlugin = GetStorePlugin();
            if (useThumbprint)
            {
                return storePlugin.FindByThumbprint(thumbprint);
            }
            else
            {
                var friendlyName = Binding.Host;
                return storePlugin.FindByFriendlyName(friendlyName);
            }
        }
    }
}
