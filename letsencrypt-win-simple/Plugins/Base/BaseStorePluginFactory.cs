using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.Base
{
    /// <summary>
    /// StorePluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseStorePluginFactory<T> : BasePluginFactory<T>, IStorePluginFactory where T : IStorePlugin
    {
        public BaseStorePluginFactory(ILogService log, string name, string description = null) : base(log, name, description) { }
    }

}
