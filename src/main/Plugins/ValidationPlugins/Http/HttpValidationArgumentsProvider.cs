using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    class HttpValidationArgumentsProvider :
        BaseArgumentsProvider<HttpValidationArguments>
    {
        public override string Group => "Validation";
        public override string Name => "HTTP validation";
        public override string Condition => "--validationmode http-01 --validation filesystem|ftp|sftp|webdav";

        public override void Configure(FluentCommandLineParser<HttpValidationArguments> parser)
        {
            parser.Setup(o => o.WebRoot)
                .As("webroot")
                .WithDescription("Root path of the site that will serve the HTTP validation requests.");
            parser.Setup(o => o.Warmup)
                .As("warmup")
                .WithDescription("Warm up website(s) before attempting HTTP validation.");
            parser.Setup(o => o.ManualTargetIsIIS)
                .As("manualtargetisiis")
                .WithDescription("Copy default web.config to the .well-known directory?");
        }

        public override bool Active(HttpValidationArguments current)
        {
            return !string.IsNullOrEmpty(current.WebRoot) ||
                current.ManualTargetIsIIS ||
                current.Warmup;
        }
    }
}
