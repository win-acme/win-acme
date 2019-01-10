using Fclp;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    abstract class HttpValidationArgumentsProvider<T> :
        BaseArgumentsProvider<T>
        where T : HttpValidationArguments, new()
    {
        public override string Group => "Validation";
        public override void Configure(FluentCommandLineParser<T> parser)
        {
            parser.Setup(o => o.WebRoot)
                .As("webroot")
                .WithDescription("A web root to use for HTTP validation.");
            parser.Setup(o => o.Warmup)
                .As("warmup")
                .WithDescription("Warm up websites before attempting HTTP validation.");
            parser.Setup(o => o.ManualTargetIsIIS)
                .As("manualtargetisiis")
                .WithDescription("Will the HTTP validation be handled by IIS?");
        }

        public override bool Validate(ILogService log, T current, MainArguments main)
        {
            var active = 
                !string.IsNullOrEmpty(current.WebRoot) || 
                current.Warmup;
            if (main.Renew && active)
            {
                log.Error("Validation parameters cannot be changed during a renewal. Recreate/overwrite the renewal or edit the .json file if you want to make changes.");
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
