using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
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
        Type IPluginOptionsFactory.InstanceType => typeof(NullInstallation);
        Type IPluginOptionsFactory.OptionsType => typeof(NullInstallationOptions);
        Task<InstallationPluginOptions?> Generate() => Task.FromResult<InstallationPluginOptions?>(new NullInstallationOptions());
        Task<InstallationPluginOptions?> IInstallationPluginOptionsFactory.Aquire(Target target, IInputService inputService, RunLevel runLevel) => Generate();
        Task<InstallationPluginOptions?> IInstallationPluginOptionsFactory.Default(Target target) => Generate();
        (bool, string?) IInstallationPluginOptionsFactory.CanInstall(IEnumerable<Type> storeTypes, IEnumerable<Type> installationTypes) => (true, null);
        int IPluginOptionsFactory.Order => int.MaxValue;
        string IPluginOptionsFactory.Name => new NullInstallationOptions().Name;
        string IPluginOptionsFactory.Description => new NullInstallationOptions().Description;
        bool IPluginOptionsFactory.Match(string name) => string.Equals(name, new NullInstallationOptions().Name, StringComparison.InvariantCultureIgnoreCase);
    }

    internal class NullInstallationOptions : InstallationPluginOptions<NullInstallation>
    {
        public override string Name => "None";
        public override string Description => "No (additional) installation steps";
    }

    [IPlugin.Plugin<NullInstallationOptions, NullInstallationOptionsFactory, WacsJson>
    ("aecc502c-5f75-43d2-b578-f95d50c79ea1", "None", "No (additional) installation steps")]
    internal class NullInstallation : IInstallationPlugin
    {
        Task<bool> IInstallationPlugin.Install(Target target, IEnumerable<IStorePlugin> stores, CertificateInfo newCertificateInfo, CertificateInfo? oldCertificateInfo) => Task.FromResult(true);
    }
}
