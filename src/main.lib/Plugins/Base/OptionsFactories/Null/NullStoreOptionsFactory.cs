using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullStoreOptionsFactory : IStorePluginOptionsFactory
    {
        Type IPluginOptionsFactory.InstanceType => typeof(NullStore);
        Type IPluginOptionsFactory.OptionsType => typeof(NullStoreOptions);
        Task<StorePluginOptions?> Generate() => Task.FromResult<StorePluginOptions?>(new NullStoreOptions());
        Task<StorePluginOptions?> IPluginOptionsFactory<StorePluginOptions>.Aquire(IInputService inputService, RunLevel runLevel) => Generate();
        Task<StorePluginOptions?> IPluginOptionsFactory<StorePluginOptions>.Default() => Generate();
        int IPluginOptionsFactory.Order => int.MaxValue;
    }

    /// <summary>
    /// Do not make INull, we actually need to store these options to override
    /// the default behaviour of CertificateStore
    /// </summary>
    internal class NullStoreOptions : StorePluginOptions<NullStore> {}

    [IPlugin.Plugin<NullStoreOptions, NullStoreOptionsFactory, WacsJsonPlugins>
        ("cfdd7caa-ba34-4e9e-b9de-2a3d64c4f4ec", Name, "No (additional) store steps")]
    internal class NullStore : IStorePlugin
    {
        public const string Name = "None";
        public Task Delete(CertificateInfo certificateInfo) => Task.CompletedTask;
        public Task Save(CertificateInfo certificateInfo) {
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
