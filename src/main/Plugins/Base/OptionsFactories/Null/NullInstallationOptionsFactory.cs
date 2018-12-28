using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullInstallationFactory : IInstallationPluginOptionsFactory, INull
    {
        Type IHasType.Instance => typeof(NullInstallation);
        InstallationPluginOptions IInstallationPluginOptionsFactory.Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel) => new NullInstallationOptions();
        InstallationPluginOptions IInstallationPluginOptionsFactory.Default(Target target, IOptionsService optionsService) => new NullInstallationOptions();
        bool IInstallationPluginOptionsFactory.CanInstall() => true;
        string IHasName.Name => (new NullInstallationOptions()).Name;
        string IHasName.Description => (new NullInstallationOptions()).Description;
        bool IHasName.Match(string name) => string.Equals(name, (new NullInstallationOptions()).Name, StringComparison.CurrentCultureIgnoreCase);
    }

    internal class NullInstallationOptions : InstallationPluginOptions<NullInstallation>
    {
        public override string Name => "None";
        public override string Description => null;
    }

    internal class NullInstallation : IInstallationPlugin
    {
        void IInstallationPlugin.Install(CertificateInfo newCertificateInfo, CertificateInfo oldCertificateInfo) { }
    }
}
