using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class DreamhostOptionsFactory : ValidationPluginOptionsFactory<DreamhostDnsValidation, DreamhostOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public DreamhostOptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        public override async Task<DreamhostOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var apiKey = await _arguments.GetProtectedString<DreamhostArguments>(x => x.ApiKey).
                Interactive(input).
                Required().
                GetValue();
            return new DreamhostOptions()
            {
                ApiKey = apiKey
            };
        }

        public override async Task<DreamhostOptions> Default(Target target)
        {
            var apiKey = await _arguments.GetProtectedString<DreamhostArguments>(x => x.ApiKey).
                Required().
                GetValue();
            return new DreamhostOptions()
            {
                ApiKey = apiKey
            };
        }

        public override bool CanValidate(Target target) => true;
    }
}
