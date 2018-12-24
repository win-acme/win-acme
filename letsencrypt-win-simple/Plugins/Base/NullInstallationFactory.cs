using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullInstallationFactory : IInstallationPluginFactory, INull
    {
        public const string PluginName = "None";
        string IHasName.Name => PluginName;
        string IHasName.Description => "Do not run any installation steps";
        Type IHasType.Instance => typeof(NullInstallation);
        bool IInstallationPluginFactory.CanInstall(ScheduledRenewal renewal) => true;
        bool IHasName.Match(string name) => string.Equals("None", name, StringComparison.InvariantCultureIgnoreCase);
        void IInstallationPluginFactory.Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel) { }
        void IInstallationPluginFactory.Default(ScheduledRenewal renewal, IOptionsService optionsService) { }
    }

    internal class NullInstallation : IInstallationPlugin
    {
        void IInstallationPlugin.Install(CertificateInfo newCertificateInfo, CertificateInfo oldCertificateInfo) { }
    }
}
