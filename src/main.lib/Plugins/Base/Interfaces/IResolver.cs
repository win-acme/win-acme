using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    internal interface IResolver
    {
        Task<PluginFrontend<IPluginCapability, TargetPluginOptions>?> GetTargetPlugin();

        Task<PluginFrontend<IValidationPluginCapability, ValidationPluginOptions>?> GetValidationPlugin();
       
        Task<PluginFrontend<IOrderPluginCapability, OrderPluginOptions>?> GetOrderPlugin();

        Task<PluginFrontend<IPluginCapability, CsrPluginOptions>?> GetCsrPlugin();

        Task<PluginFrontend<IPluginCapability, StorePluginOptions>?> GetStorePlugin(IEnumerable<Plugin> chosen);

        Task<PluginFrontend<IInstallationPluginCapability, InstallationPluginOptions>?> GetInstallationPlugin(IEnumerable<Plugin> storeType, IEnumerable<Plugin> chosen);
    }
}