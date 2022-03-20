using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    class SiteOptionsFactory : OrderPluginOptionsFactory<Site, SiteOptions>
    {
        public override bool CanProcess(Target target) => target.UserCsrBytes == null;
        public override Task<SiteOptions> Aquire(IInputService inputService, RunLevel runLevel) => Default();
        public override Task<SiteOptions> Default() => Task.FromResult(new SiteOptions());
    }
}
