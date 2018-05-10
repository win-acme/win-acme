using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base
{
    public abstract class BasePluginFactory<T> : IHasName, IHasType
    {
        protected string _name;
        protected string _description;
        protected ILogService _log;

        public BasePluginFactory(ILogService log, string name, string description = null)
        {
            _log = log;
            _name = name;
            _description = description ?? name;
        }

        public virtual bool Match(string name)
        {
            return string.Equals(name, _name, StringComparison.InvariantCultureIgnoreCase);
        }

        string IHasName.Name => _name;
        string IHasName.Description => _description;
        Type IHasType.Instance => typeof(T);
    }
}
