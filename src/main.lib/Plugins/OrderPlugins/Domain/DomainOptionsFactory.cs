using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    class DomainOptionsFactory : OrderPluginOptionsFactory<Domain, DomainOptions>
    {
        public override bool CanProcess(Target target) => target.UserCsrBytes == null;
        public override Task<DomainOptions> Aquire(IInputService inputService, RunLevel runLevel) => Default();
        public override Task<DomainOptions> Default() => Task.FromResult(new DomainOptions());
    }
}
