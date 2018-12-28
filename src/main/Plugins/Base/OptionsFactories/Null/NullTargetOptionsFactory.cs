using PKISharp.WACS.Plugins.Interfaces;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullTargetFactory : ITargetPluginOptionsFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IHasType.Instance => typeof(object);
        bool IHasName.Match(string name) => false;
        bool ITargetPluginOptionsFactory.Hidden => true;
    }
}
