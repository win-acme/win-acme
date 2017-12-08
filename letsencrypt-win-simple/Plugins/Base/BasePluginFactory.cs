using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.Base
{
    abstract class BasePluginFactory<T> : IHasName, IHasType
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
        Type IHasType.Instance { get { return typeof(T); } }
    }
}
