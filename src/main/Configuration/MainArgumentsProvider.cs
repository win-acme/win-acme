using Fclp;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Configuration
{
    class MainArgumentsProvider : BaseArgumentsProvider<MainArguments>
    {
        public override string Name => "Main";
        public override string Group => "";
        public override string Condition => "";

        public override void Configure(FluentCommandLineParser<MainArguments> parser)
        {
            // Basic options
            parser.Setup(o => o.BaseUri)
                .As("baseuri")
                .WithDescription("The address of the ACME server to use.");

            parser.Setup(o => o.ImportBaseUri)
                .As("importbaseuri")
                .WithDescription("[--import] The address of the ACME server to use to import ScheduledRenewals from.");

            parser.Setup(o => o.Import)
                .As("import")
                .WithDescription("[--import] The address of the ACME server to use to import ScheduledRenewals from.");

            parser.Setup(o => o.Test)
                .As("test")
                .WithDescription("Enables testing behaviours in the program which may help with troubleshooting.");

            parser.Setup(o => o.Verbose)
                .As("verbose")
                .WithDescription("Print additional log messages to console for troubleshooting.");

            parser.Setup(o => o.Help)
                .As('?', "help")
                .WithDescription("Show information about command line options.");

            parser.Setup(o => o.Version)
                .As("version")
                .WithDescription("Show version information.");

            parser.Setup(o => o.List)
                .As("list")
                .WithDescription("List all created renewals.");

            // Renewal

            parser.Setup(o => o.Renew)
                .As("renew")
                .WithDescription("Check for scheduled renewals.");
            parser.Setup(o => o.Force)
                .As("force")
                .WithDescription("Force renewal on all scheduled certificates when used together with --renew. Otherwise just bypasses the certificate cache on new certificate requests.");

            // Commands

            parser.Setup(o => o.FriendlyName)
                .As("friendlyname")
                .WithDescription("Give the friendly name of certificate, either to be used for creating a new one or to target a command (like --cancel or --renew) at as specific one");

            parser.Setup(o => o.Cancel)
                .As("cancel")
                .WithDescription("Cancels existing scheduled renewal as specified by the target parameters.");

            // Plugins (unattended)

            parser.Setup(o => o.Target)
                .As("target")
                .WithDescription("Specify which target plugin to run, bypassing the main menu and triggering unattended mode.");

            parser.Setup(o => o.Validation)
               .As("validation")
               .WithDescription("Specify which validation plugin to run. If none is specified, FileSystem validation will be chosen as the default.");

            parser.Setup(o => o.ValidationMode)
                .As("validationmode")
                .SetDefault(Constants.Http01ChallengeType)
                .WithDescription("Specify which validation mode to use.");

            parser.Setup(o => o.Store)
                .As("store")
                .WithDescription("Specify which store plugin to use.");

            parser.Setup(o => o.Installation)
                .As("installation")
                .WithDescription("Specify which installation plugins to use. This may be a comma separated list.");

            // Remove
            parser.Setup(o => o.SiteId)
                .As("siteid")
                .WithDescription("[--target iissite|iissites|iisbinding] Specify identifier of the site that the plugin should create the target from. For the iissites plugin this may be a comma separated list.");
            parser.Setup(o => o.CommonName)
                .As("commonname")
                .WithDescription("[--target iissite|iissites|manual] Specify the common name of the certificate that should be requested for the target.");
            parser.Setup(o => o.ExcludeBindings)
                .As("excludebindings")
                .WithDescription("[--target iissite|iissites] Exclude bindings from being included in the certificate. This may be a comma separated list.");
            parser.Setup(o => o.HideHttps)
                .As("hidehttps")
                .WithDescription("Hide sites that have existing https bindings.");
            parser.Setup(o => o.Host)
                .As("host")
                .WithDescription("[--target manual|iisbinding] A host name to manually get a certificate for. For the manual plugin this may be a comma separated list.");

            // Misc

            parser.Setup(o => o.CloseOnFinish)
                .As("closeonfinish")
                .WithDescription("[--test] Close the application when complete, which usually doesn't happen in test mode.");

            parser.Setup(o => o.NoTaskScheduler)
                .As("notaskscheduler")
                .WithDescription("Do not create (or offer to update) the scheduled task.");

            parser.Setup(o => o.UseDefaultTaskUser)
                .As("usedefaulttaskuser")
                .WithDescription("Avoid the question about specifying the task scheduler user, as such defaulting to the SYSTEM account.");

            // Acme account registration

            parser.Setup(o => o.AcceptTos)
                .As("accepttos")
                .WithDescription("Accept the ACME terms of service.");

            parser.Setup(o => o.EmailAddress)
                .As("emailaddress")
                .WithDescription("Email address to use by ACME for renewal fail notices.");
        }

        public override bool Active(MainArguments current)
        {
            return !string.IsNullOrEmpty(current.CommonName) ||
                !string.IsNullOrEmpty(current.ExcludeBindings) ||
                !string.IsNullOrEmpty(current.FriendlyName) ||
                !string.IsNullOrEmpty(current.Host) ||
                !string.IsNullOrEmpty(current.Installation) ||
                !string.IsNullOrEmpty(current.SiteId) ||
                !string.IsNullOrEmpty(current.Store) ||
                !string.IsNullOrEmpty(current.Target) ||
                !string.IsNullOrEmpty(current.Validation);
        }
    }
}