using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class CloudDnsOptionsFactory : ValidationPluginOptionsFactory<CloudDns, CloudDnsOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public CloudDnsOptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        private ArgumentResult<string> ServiceAccountKey => _arguments.
            GetString<CloudDnsArguments>(a => a.ServiceAccountKey).
            Required();

        private ArgumentResult<string> ProjectId => _arguments.
            GetString<CloudDnsArguments>(a => a.ProjectId).
            Required();

        public override async Task<CloudDnsOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new CloudDnsOptions
            {
                ServiceAccountKeyPath = await ServiceAccountKey.Interactive(input, "Path to Service Account Key").GetValue(),
                ProjectId = await ProjectId.Interactive(input).GetValue()
            };
        }


        public override async Task<CloudDnsOptions> Default(Target target)
        {
            return new CloudDnsOptions
            {
                ServiceAccountKeyPath = await ServiceAccountKey.GetValue(),
                ProjectId = await ProjectId.GetValue()
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
