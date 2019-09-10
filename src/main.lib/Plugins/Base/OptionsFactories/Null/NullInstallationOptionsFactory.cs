using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullInstallationOptionsFactory : IInstallationPluginOptionsFactory, INull
    {
        Type IHasType.InstanceType => typeof(NullInstallation);
        Type IHasType.OptionsType => typeof(NullInstallationOptions);
        Task<InstallationPluginOptions> IInstallationPluginOptionsFactory.Aquire(Target target, IInputService inputService, RunLevel runLevel) => Task.FromResult<InstallationPluginOptions>(new NullInstallationOptions());
        Task<InstallationPluginOptions> IInstallationPluginOptionsFactory.Default(Target target) => Task.FromResult<InstallationPluginOptions>(new NullInstallationOptions());
        bool IInstallationPluginOptionsFactory.CanInstall(IEnumerable<Type> storeTypes) => true;
        public int Order => int.MaxValue;
        string IHasName.Name => (new NullInstallationOptions()).Name;
        string IHasName.Description => (new NullInstallationOptions()).Description;
        bool IHasName.Match(string name) => string.Equals(name, (new NullInstallationOptions()).Name, StringComparison.CurrentCultureIgnoreCase);
    }

    [Plugin("aecc502c-5f75-43d2-b578-f95d50c79ea1")]
    internal class NullInstallationOptions : InstallationPluginOptions<NullInstallation>
    {
        public override string Name => "None";
        public override string Description => "Do not run any (extra) installation steps";
    }

    internal class NullInstallation : IInstallationPlugin
    {
        Task IInstallationPlugin.Install(IEnumerable<IStorePlugin> stores, CertificateInfo newCertificateInfo, CertificateInfo oldCertificateInfo) => Task.CompletedTask;
    }
}
