using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class Route53OptionsFactory : PluginOptionsFactory<Route53Options>
    {
        private readonly ArgumentsInputService _arguments;

        public Route53OptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> AccessKey => _arguments.
            GetProtectedString<Route53Arguments>(a => a.Route53SecretAccessKey).
            Required();

        private ArgumentResult<string?> AccessKeyId => _arguments.
            GetString<Route53Arguments>(a => a.Route53AccessKeyId).
            Required();

        private ArgumentResult<string?> IamRole => _arguments.
            GetString<Route53Arguments>(a => a.Route53IAMRole);

        public override async Task<Route53Options?> Aquire(IInputService input, RunLevel runLevel)
        {
            var options = new Route53Options
            {
                IAMRole = await IamRole.Interactive(input, "IAM role (blank for default)").GetValue()
            };
            if (!string.IsNullOrWhiteSpace(options.IAMRole))
            {
                return options;
            }
            options.AccessKeyId = await AccessKeyId.Interactive(input, "Access key ID").GetValue();
            options.SecretAccessKey = await AccessKey.Interactive(input, "Secret access key").GetValue();
            return options;
        }

        public override async Task<Route53Options?> Default()
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

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(Route53Options options)
        {
            yield return (IamRole.Meta, options.IAMRole);
            yield return (AccessKeyId.Meta, options.AccessKeyId);
            yield return (AccessKey.Meta, options.SecretAccessKey);
        }
    }
}
