using Fclp;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    class FileSystemArgumentsProvider : HttpValidationArgumentsProvider<FileSystemArguments>
    {
        public override string Name => "FileSystem";
        public override string Condition => "[--validationmode http-01 --validation filesystem]";

        public override void Configure(FluentCommandLineParser<FileSystemArguments> parser)
        {
            base.Configure(parser);
            parser.Setup(o => o.ValidationSiteId)
                .As("validationsiteid")
                .WithDescription("Specify site to use for handling validation requests.");
        }

        public override bool Validate(ILogService log, FileSystemArguments current, MainArguments main)
        {
            var active = current.ValidationSiteId != null;
            if (main.Renew && active)
            {
                log.Error("Validation parameters cannot be changed during a renewal. Recreate/overwrite the renewal or edit the .json file if you want to make changes.");
                return false;
            }
            else
            {
                return base.Validate(log, current, main);
            }
        }
    }
}
