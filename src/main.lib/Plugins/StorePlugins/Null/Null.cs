using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [IPlugin.Plugin<
        NullOptions, NullOptionsFactory, 
        DefaultCapability, WacsJsonPlugins>
        ("cfdd7caa-ba34-4e9e-b9de-2a3d64c4f4ec",
        Name, "No (additional) store steps")]
    internal class Null : IStorePlugin
    {
        internal const string Name = "None";
        public Task Delete(ICertificateInfo certificateInfo) => Task.CompletedTask;
        public Task Save(ICertificateInfo certificateInfo) {
            certificateInfo.StoreInfo.Add(GetType(),
                    new StoreInfo()
                    {
                        Name = "None",
                        Path = ""
                    });
            return Task.CompletedTask;
        }
    }

}
