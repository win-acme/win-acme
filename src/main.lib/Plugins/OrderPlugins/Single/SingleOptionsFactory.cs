using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    class SingleOptionsFactory : OrderPluginOptionsFactory<Single, SingleOptions>
    {
        public override Task<SingleOptions> Aquire(IInputService inputService, RunLevel runLevel) => Default();
        public override Task<SingleOptions> Default() => Task.FromResult(new SingleOptions());
    }
}
