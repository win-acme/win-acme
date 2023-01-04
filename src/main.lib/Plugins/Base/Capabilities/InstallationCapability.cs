using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System;

namespace PKISharp.WACS.Plugins.Base.Capabilities
{
    public class InstallationCapability : DefaultCapability, IInstallationPluginCapability
    {
        public virtual (bool, string?) CanInstall(IEnumerable<Type> storeTypes, IEnumerable<Type> installationTypes) => (true, null);
    }
}
