using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.Base
{
    /// <summary>
    /// TargetPluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class BaseTargetPluginFactory<T> : BasePluginFactory<T>, ITargetPluginFactory where T : ITargetPlugin
    {
        public BaseTargetPluginFactory(ILogService log, string name, string description = null) : base(log, name, description) { }
    }
}
