using Fclp;
using PKISharp.WACS.Plugins.TargetPlugins;

namespace PKISharp.WACS.Configuration.Arguments
{
    internal class MainArgumentsProvider : BaseArgumentsProvider<MainArguments>
    {
        public override string Name => "Main";
        public override string Group => "";
        public override string Condition => "";

        protected override bool IsActive(MainArguments current)
        {
            return
                !string.IsNullOrEmpty(current.FriendlyName) ||
                !string.IsNullOrEmpty(current.Installation) ||
                !string.IsNullOrEmpty(current.Store) ||
                !string.IsNullOrEmpty(current.Order) ||
                !string.IsNullOrEmpty(current.Csr) ||
                !string.IsNullOrEmpty(current.Target) ||
                !string.IsNullOrEmpty(current.Validation);
        }

        public override void Configure(FluentCommandLineParser<MainArguments> parser)
        {
            // Basic options
            parser.Setup(o => o.BaseUri)
                .As("baseuri")
                .WithDescription("Address of the ACMEv2 server to use. The default endpoint can be modified in settings.json.");

            parser.Setup(o => o.Import)
                .As("import")
                .WithDescription("Import scheduled renewals from version 1.9.x in unattended mode.");

            parser.Setup(o => o.ImportBaseUri)
                .As("importbaseuri")
                .WithDescription("[--import] When importing scheduled renewals from version 1.9.x, this argument can change the address of the ACMEv1 server to import from. The default endpoint to import from can be modified in settings.json.");

            parser.Setup(o => o.Test)
                .As("test")
                .WithDescription("Enables testing behaviours in the program which may help with troubleshooting. By default this also switches the --baseuri to the ACME test endpoint. The default endpoint for test mode can be modified in settings.json.");

            parser.Setup(o => o.Verbose)
                .As("verbose")
                .WithDescription("Print additional log messages to console for troubleshooting and bug reports.");

            parser.Setup(o => o.Help)
                .As('?', "help")
                .WithDescription("Show information about all available command line options.");

            parser.Setup(o => o.Version)
                .As("version")
                .WithDescription("Show version information.");


            // Renewal

            parser.Setup(o => o.Renew)
                .As("renew")
                .WithDescription("Renew any certificates that are due. This argument is used by the scheduled task. Note that it's not possible to change certificate properties and renew at the same time.");
            parser.Setup(o => o.Force)
                .As("force")
                .WithDescription("Force renewal when used together with --renew. Otherwise bypasses the certificate cache on new certificate requests.");

            // Commands

            parser.Setup(o => o.Cancel)
                .As("cancel")
                .WithDescription("Cancel renewal specified by the --friendlyname or --id arguments.");
            parser.Setup(o => o.Revoke)
                .As("revoke")
                .WithDescription("Revoke the most recently issued certificate for the renewal specified by the --friendlyname or --id arguments.");

            parser.Setup(o => o.List)
                .As("list")
                .WithDescription("List all created renewals in unattended mode.");

            // Targeting

            parser.Setup(o => o.Id)
                .As("id")
                .WithDescription("[--target|--cancel|--renew|--revoke] Id of a new or existing renewal, can be used to override the default when creating a new renewal or to specify a specific renewal for other commands.");

            parser.Setup(o => o.FriendlyName)
                .As("friendlyname")
                .WithDescription("[--target|--cancel|--renew|--revoke] Friendly name of a new or existing renewal, can be used to override the default when creating a new renewal or to specify a specific renewal for other commands. In the latter case a pattern might be used. " + IISArgumentsProvider.PatternExamples);

            // Plugins (unattended)

            parser.Setup(o => o.Target)
                .As("target")
                .WithDescription("Specify which target plugin to run, bypassing the main menu and triggering unattended mode.");

            parser.Setup(o => o.Validation)
               .As("validation")
               .WithDescription("Specify which validation plugin to run. If none is specified, SelfHosting validation will be chosen as the default.");

            parser.Setup(o => o.ValidationMode)
                .As("validationmode")
                .SetDefault(Constants.Http01ChallengeType)
                .WithDescription("Specify which validation mode to use. HTTP-01 is the default.");
          
            parser.Setup(o => o.Order)
                .As("order")
                .WithDescription("Specify which order plugin to use. Single is the default.");

            parser.Setup(o => o.Csr)
                .As("csr")
                .WithDescription("Specify which CSR plugin to use. RSA is the default.");

            parser.Setup(o => o.Store)
                .As("store")
                .WithDescription("Specify which store plugin to use. CertificateStore is the default. This may be a comma-separated list.");

            parser.Setup(o => o.Installation)
                .As("installation")
                .WithDescription("Specify which installation plugins to use. IIS is the default. This may be a comma-separated list.");

            // Misc

            parser.Setup(o => o.CloseOnFinish)
                .As("closeonfinish")
                .WithDescription("[--test] Close the application when complete, which usually does not happen when test mode is active. Useful to test unattended operation.");

            parser.Setup(o => o.HideHttps)
                .As("hidehttps")
                .WithDescription("Hide sites that have existing https bindings from interactive mode.");

            parser.Setup(o => o.NoTaskScheduler)
                .As("notaskscheduler")
                .WithDescription("Do not create (or offer to update) the scheduled task.");

            parser.Setup(o => o.UseDefaultTaskUser)
                .As("usedefaulttaskuser")
                .WithDescription("(Obsolete) Avoid the question about specifying the task scheduler user, as such defaulting to the SYSTEM account.");

            parser.Setup(o => o.Encrypt)
                .As("encrypt")
                .WithDescription("Rewrites all renewal information using current EncryptConfig setting");

            parser.Setup(o => o.SetupTaskScheduler)
                .As("setuptaskscheduler")
                .WithDescription("Create or update the scheduled task according to the current settings.");
        }
    }
}