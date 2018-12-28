using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// TargetPluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseTargetPluginFactory<T> : BasePluginFactory<T>, ITargetPluginOptionsFactory where T : ITargetPlugin
    {
        public BaseTargetPluginFactory(ILogService log, string name, string description = null) : base(log, name, description) { }

        /// <summary>
        /// Allow implementations to hide themselves from users
        /// in interactive mode
        /// </summary>
        public virtual bool Hidden => false;
    }
}
