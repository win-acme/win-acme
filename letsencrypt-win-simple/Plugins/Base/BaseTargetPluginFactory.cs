using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base
{
    /// <summary>
    /// TargetPluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseTargetPluginFactory<T> : BasePluginFactory<T>, ITargetPluginFactory where T : ITargetPlugin
    {
        public BaseTargetPluginFactory(ILogService log, string name, string description = null) : base(log, name, description) { }

        /// <summary>
        /// Allow implementations to hide themselves from users
        /// in interactive mode
        /// </summary>
        public virtual bool Hidden => false;
    }
}
