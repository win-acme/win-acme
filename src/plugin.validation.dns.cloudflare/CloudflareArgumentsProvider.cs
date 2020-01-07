using Fclp;
using PKISharp.WACS.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class CloudflareArgumentsProvider : BaseArgumentsProvider<CloudflareArguments>
    {
        public override string Name => "Cloudflare";

        public override string Group => "Validation";

        public override string Condition => "--validationmode dns-01 --validation cloudflare";

        public override void Configure(FluentCommandLineParser<CloudflareArguments> parser)
        {
            parser.Setup(o => o.CloudflareApiToken)
                .As("cloudflareapitoken")
                .WithDescription("API Token for Cloudflare.");
        }
    }
}
