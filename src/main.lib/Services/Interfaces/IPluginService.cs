using Autofac;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface IPluginService
    {
        IEnumerable<Plugin> GetPlugins();
        IEnumerable<Plugin> GetPlugins(Steps step);
        Plugin? GetPlugin(Guid id);
        Plugin? GetPlugin(ILifetimeScope scope, Steps step, string name, string? parameter = null);
    }
}
