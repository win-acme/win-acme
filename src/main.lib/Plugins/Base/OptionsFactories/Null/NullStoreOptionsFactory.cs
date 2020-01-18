using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullStoreOptionsFactory : IStorePluginOptionsFactory, INull
    {
        Type IPluginOptionsFactory.InstanceType => typeof(NullStore);
        Type IPluginOptionsFactory.OptionsType => typeof(NullStoreOptions);
        Task<StorePluginOptions?> Generate() => Task.FromResult<StorePluginOptions?>(new NullStoreOptions());
        Task<StorePluginOptions?> IStorePluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel) => Generate();
        Task<StorePluginOptions?> IStorePluginOptionsFactory.Default() => Generate();
        bool IPluginOptionsFactory.Disabled => false;
        string IPluginOptionsFactory.Name => new NullStoreOptions().Name;
        string IPluginOptionsFactory.Description => new NullStoreOptions().Description;
        bool IPluginOptionsFactory.Match(string name) => string.Equals(name, new NullInstallationOptions().Name, StringComparison.CurrentCultureIgnoreCase);
        int IPluginOptionsFactory.Order => int.MaxValue;
    }

    [Plugin("cfdd7caa-ba34-4e9e-b9de-2a3d64c4f4ec")]
    internal class NullStoreOptions : StorePluginOptions<NullStore>
    {
        public override string Name => "None";
        public override string Description => "No (additional) installation steps";
    }

    internal class NullStore : IStorePlugin
    {
        bool IPlugin.Disabled => false;
        public Task Delete(CertificateInfo certificateInfo) => Task.CompletedTask;
        public Task Save(CertificateInfo certificateInfo) => Task.CompletedTask;
    }

}
