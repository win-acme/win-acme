using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base
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
