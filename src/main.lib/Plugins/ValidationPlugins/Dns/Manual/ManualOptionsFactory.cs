using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class ManualOptionsFactory : ValidationPluginOptionsFactory<Manual, ManualOptions>
    {
        public ManualOptionsFactory() : base(Constants.Dns01ChallengeType) { }
        public override Task<ManualOptions?> Aquire(Target target, IInputService inputService, RunLevel runLevel) => Task.FromResult<ManualOptions?>(new ManualOptions());
        public override Task<ManualOptions?> Default(Target target) => Task.FromResult<ManualOptions?>(new ManualOptions());
        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
