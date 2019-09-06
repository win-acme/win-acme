using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class Route53OptionsFactory : ValidationPluginOptionsFactory<Route53, Route53Options>
    {
        private readonly IArgumentsService _arguments;

        public Route53OptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType)
        {
            _arguments = arguments;
        }

        public override Route53Options Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<Route53Arguments>();
            var options = new Route53Options
            {
                IAMRole = _arguments.TryGetArgument(args.Route53IAMRole, input, "AWS IAM role for current EC2 instance (blank for default)")
            };

            if (!string.IsNullOrWhiteSpace(options.IAMRole))
                return options;

            options.AccessKeyId = _arguments.TryGetArgument(args.Route53AccessKeyId, input, "AWS access key ID");
            options.SecretAccessKey = new ProtectedString(_arguments.TryGetArgument(args.Route53SecretAccessKey, input, "AWS secret access key", true));

            return options;
        }

        public override Route53Options Default(Target target)
        {
            var args = _arguments.GetArguments<Route53Arguments>();
            if (!string.IsNullOrEmpty(args.Route53IAMRole))
            {
                return new Route53Options
                {
                    IAMRole = args.Route53IAMRole
                };
            }

            return new Route53Options
            {
                AccessKeyId = _arguments.TryGetRequiredArgument(nameof(args.Route53AccessKeyId), args.Route53AccessKeyId),
                SecretAccessKey = new ProtectedString(_arguments.TryGetRequiredArgument(nameof(args.Route53SecretAccessKey), args.Route53SecretAccessKey))
            };
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
