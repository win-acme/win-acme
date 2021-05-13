using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class Route53OptionsFactory : ValidationPluginOptionsFactory<Route53, Route53Options>
    {
        private readonly ArgumentsInputService _arguments;

        public Route53OptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        private ArgumentResult<Route53Arguments, ProtectedString> AccessKey => _arguments.
            GetProtectedString<Route53Arguments>(a => a.Route53SecretAccessKey).
            Required();

        private ArgumentResult<Route53Arguments, string> AccessKeyId => _arguments.
            GetString<Route53Arguments>(a => a.Route53AccessKeyId).
            Required();

        private ArgumentResult<Route53Arguments, string> IamRole => _arguments.
            GetString<Route53Arguments>(a => a.Route53IAMRole);

        public override async Task<Route53Options> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var options = new Route53Options
            {
                IAMRole = await IamRole.Interactive(input, "AWS IAM role for current EC2 instance (blank for default)").GetValue()
            };
            if (!string.IsNullOrWhiteSpace(options.IAMRole))
            {
                return options;
            }
            options.AccessKeyId = await AccessKeyId.Interactive(input, "AWS access key ID").GetValue();
            options.SecretAccessKey = await AccessKey.Interactive(input, "AWS secret access key").GetValue();
            return options;
        }

        public override async Task<Route53Options> Default(Target target)
        {
            var options = new Route53Options
            {
                IAMRole = await IamRole.GetValue()
            };
            if (!string.IsNullOrWhiteSpace(options.IAMRole))
            {
                return options;
            }
            options.AccessKeyId = await AccessKeyId.GetValue();
            options.SecretAccessKey = await AccessKey.GetValue();
            return options;
        }

        public override bool CanValidate(Target target) => true;
    }
}
