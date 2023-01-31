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
        Name, "No (additional) installation steps")]
    internal class Null : IInstallationPlugin
    {
        internal const string Name = "None";
        Task<bool> IInstallationPlugin.Install(Dictionary<Type, StoreInfo> storeInfo, ICertificateInfo newCertificateInfo, ICertificateInfo? oldCertificateInfo) => Task.FromResult(true);
    }
}
