using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class CloudDnsOptionsFactory : ValidationPluginOptionsFactory<CloudDns, CloudDnsOptions>
    {
        private readonly IArgumentsService _arguments;

        public CloudDnsOptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        public override async Task<CloudDnsOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var options = new CloudDnsOptions();
            var args = _arguments.GetArguments<CloudDnsArguments>();
            options.ServiceAccountKeyPath = await _arguments.TryGetArgument(args.ServiceAccountKey, input, "Path to Service Account Key");
            options.ProjectId = await _arguments.TryGetArgument(args.ProjectId, input, "Project Id");
            return options;
        }


        public override Task<CloudDnsOptions> Default(Target target)
        {
            var args = _arguments.GetArguments<CloudDnsArguments>();
            return Task.FromResult(new CloudDnsOptions
            {
                ServiceAccountKeyPath = _arguments.TryGetRequiredArgument(nameof(args.ServiceAccountKey), args.ServiceAccountKey),
                ProjectId = _arguments.TryGetRequiredArgument(nameof(args.ProjectId), args.ProjectId)
            });
        }

        public override bool CanValidate(Target target) => true;
    }
}
