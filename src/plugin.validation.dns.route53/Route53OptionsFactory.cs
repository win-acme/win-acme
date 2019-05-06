using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class Route53OptionsFactory : ValidationPluginOptionsFactory<Route53, Route53Options>
    {
        public Route53OptionsFactory(ILogService log)
            : base(log, Dns01ChallengeValidationDetails.Dns01ChallengeType) {}

        public override Route53Options Aquire(Target target, IArgumentsService arguments, IInputService input, RunLevel runLevel)
        {
            var args = arguments.GetArguments<Route53Arguments>();
            var options = new Route53Options
            {
                IAMRole = arguments.TryGetArgument(args.Route53IAMRole, input, "AWS IAM role for current EC2 instance (blank for default)")
            };

            if (!string.IsNullOrWhiteSpace(options.IAMRole))
                return options;

            options.AccessKeyId = arguments.TryGetArgument(args.Route53AccessKeyId, input, "AWS access key ID");
            options.SecretAccessKey = arguments.TryGetArgument(args.Route53SecretAccessKey, input, "AWS secret access key", true);

            return options;
        }

        public override Route53Options Default(Target target, IArgumentsService arguments)
        {
            var args = arguments.GetArguments<Route53Arguments>();

            return new Route53Options
            {
                AccessKeyId = arguments.TryGetRequiredArgument(nameof(args.Route53AccessKeyId), args.Route53AccessKeyId),
                SecretAccessKey = arguments.TryGetRequiredArgument(nameof(args.Route53SecretAccessKey), args.Route53SecretAccessKey)
            };
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
