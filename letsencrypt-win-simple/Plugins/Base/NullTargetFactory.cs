using PKISharp.WACS.Plugins.Interfaces;
using System;

namespace PKISharp.WACS.Plugins.Base
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullTargetFactory : ITargetPluginFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IHasType.Instance => typeof(object);
        bool IHasName.Match(string name) => false;
        bool ITargetPluginFactory.Hidden => true;
    }
}
