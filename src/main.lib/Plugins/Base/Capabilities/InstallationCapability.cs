using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System;

namespace PKISharp.WACS.Plugins.Base.Capabilities
{
    public class InstallationCapability : DefaultCapability, IInstallationPluginCapability
    {
        public virtual State CanInstall(IEnumerable<Type> storeTypes, IEnumerable<Type> installationTypes) => State.EnabledState();
    }
}
