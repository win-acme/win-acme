using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class CloudflareOptionsFactory : ValidationPluginOptionsFactory<Cloudflare, CloudflareOptions>
    {
        private readonly IArgumentsService _arguments;
        public CloudflareOptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        public override async Task<CloudflareOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            var arg = _arguments.GetArguments<CloudflareArguments>();
            var opts = new CloudflareOptions
            {
                ApiToken = new ProtectedString(await _arguments.TryGetArgument(arg.CloudflareApiToken, inputService, "Cloudflare API Token", true))
            };
            return opts;
        }

        public override Task<CloudflareOptions> Default(Target target)
        {
            var arg = _arguments.GetArguments<CloudflareArguments>();
            var opts = new CloudflareOptions
            {
                ApiToken = new ProtectedString(_arguments.TryGetRequiredArgument(nameof(arg.CloudflareApiToken), arg.CloudflareApiToken))
            };
            return Task.FromResult(opts);
        }

        public override bool CanValidate(Target target) => true;
    }
}
