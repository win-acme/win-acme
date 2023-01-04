using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    internal interface IResolver
    {
        Task<PluginFrontend<PluginOptionsFactory<TargetPluginOptions>, IPluginCapability>?> GetTargetPlugin();

        Task<PluginFrontend<PluginOptionsFactory<ValidationPluginOptions>, IValidationPluginCapability>?> GetValidationPlugin();
       
        Task<PluginFrontend<PluginOptionsFactory<OrderPluginOptions>, IOrderPluginCapability>?> GetOrderPlugin();

        Task<PluginFrontend<PluginOptionsFactory<CsrPluginOptions>, IPluginCapability>?> GetCsrPlugin();

        Task<PluginFrontend<PluginOptionsFactory<StorePluginOptions>, IPluginCapability>?> GetStorePlugin(IEnumerable<Plugin> chosen);

        Task<PluginFrontend<PluginOptionsFactory<InstallationPluginOptions>, IInstallationPluginCapability>?> GetInstallationPlugin(IEnumerable<Plugin> storeType, IEnumerable<Plugin> chosen);
    }
}