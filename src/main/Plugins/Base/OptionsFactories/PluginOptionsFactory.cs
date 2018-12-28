using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    public abstract class PluginOptionsFactory<TPlugin, TOptions> : 
        IPluginOptionsFactory
        where TOptions : PluginOptions, new()
    {
        protected ILogService _log;
        private readonly string _name;
        private readonly string _description;

        public PluginOptionsFactory(ILogService log)
        {
            _log = log;
            var protoType = new TOptions();
            _name = protoType.Name;
            _description = protoType.Description;
            if (protoType.Instance.FullName != typeof(TPlugin).FullName)
            {
                throw new Exception();
            }
        }

        string IHasName.Name => _name;
        string IHasName.Description => _description;
        bool IHasName.Match(string name) => string.Equals(name, _name, StringComparison.CurrentCultureIgnoreCase);

        Type IHasType.Instance => typeof(TPlugin);
    }
}
