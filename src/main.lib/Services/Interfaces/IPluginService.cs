using PKISharp.WACS.Plugins;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PKISharp.WACS.Services
{
    public interface IPluginService
    {
        IEnumerable<Plugin> GetPlugins();
        IEnumerable<Plugin> GetPlugins(Steps step);
        IEnumerable<BasePlugin> GetSecretServices();
        IEnumerable<BasePlugin> GetNotificationTargets();
        bool TryGetPlugin([NotNullWhen(true)] PluginOptionsBase? options, [NotNullWhen(true)] out Plugin? plugin);
        Plugin GetPlugin(PluginOptionsBase options);
        Plugin? GetPlugin(Steps step, string name, string? parameter = null);
    }
}
