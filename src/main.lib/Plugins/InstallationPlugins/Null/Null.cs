using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [IPlugin.Plugin<
        NullOptions, NullOptionsFactory,
        InstallationCapability, WacsJsonPlugins>
        ("aecc502c-5f75-43d2-b578-f95d50c79ea1", 
        "None", "No (additional) installation steps")]
    internal class Null : IInstallationPlugin
    {
        Task<bool> IInstallationPlugin.Install(IEnumerable<Type> stores, CertificateInfo newCertificateInfo, CertificateInfo? oldCertificateInfo) => Task.FromResult(true);
    }
}
