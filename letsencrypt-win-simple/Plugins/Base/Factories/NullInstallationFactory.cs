using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullInstallationFactory : IInstallationPluginFactory, INull
    {
        string IHasName.Name => "None";
        bool IHasName.Match(string name) => string.Equals("None", name, StringComparison.InvariantCultureIgnoreCase);
        string IHasName.Description => "Do not run any installation steps";

        Type IHasType.Instance => typeof(NullInstallation);

        InstallationPluginOptions IInstallationPluginFactory.Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel) => new NullInstallationOptions();
        InstallationPluginOptions IInstallationPluginFactory.Default(ScheduledRenewal renewal, IOptionsService optionsService) => new NullInstallationOptions();
        bool IInstallationPluginFactory.CanInstall() => true;
    }

    internal class NullInstallationOptions : InstallationPluginOptions<NullInstallation> { }

    internal class NullInstallation : IInstallationPlugin
    {
        void IInstallationPlugin.Install(CertificateInfo newCertificateInfo, CertificateInfo oldCertificateInfo) { }
    }
}
