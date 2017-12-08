using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.Base
{
    /// <summary>
    /// Null implementation
    /// </summary>
    class NullTargetFactory : ITargetPluginFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IHasType.Instance => typeof(object);
        bool IHasName.Match(string name) => false;
    }
}
