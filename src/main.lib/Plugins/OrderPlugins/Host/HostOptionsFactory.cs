using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    class HostOptionsFactory : OrderPluginOptionsFactory<Host, HostOptions>
    {
        public override bool CanProcess(Target target) => target.UserCsrBytes == null;
        public override Task<HostOptions> Aquire(IInputService inputService, RunLevel runLevel) => Default();
        public override Task<HostOptions> Default() => Task.FromResult(new HostOptions());
    }
}
