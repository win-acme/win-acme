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
            parser.Setup(o => o.Test)
                .As("test")
                .WithDescription("Enables testing behaviours in the program which may help with troubleshooting.");
            parser.Setup(o => o.Import)
                .As("import")
                .WithDescription("[--import] The address of the ACME server to use to import ScheduledRenewals from.");
            parser.Setup(o => o.ImportBaseUri)
                .As("importbaseuri")
                .WithDescription("[--import] The address of the ACME server to use to import ScheduledRenewals from.");
            parser.Setup(o => o.Verbose)
                .As("verbose")
                .WithDescription("Print additional log messages to console for troubleshooting.");
            parser.Setup(o => o.Help)
                .As('?', "help")
                .WithDescription("Show information about command line options.");
            parser.Setup(o => o.Version)
                .As("version")
                .WithDescription("Show version information.");

            // Main menu actions
            parser.Setup(o => o.Renew)
                .As("renew")
                .WithDescription("Check for scheduled renewals.");
            parser.Setup(o => o.Force)
                .As("force")
                .WithDescription("Force renewal on all scheduled certificates when used together with --renew. Otherwise just bypasses the certificate cache on new certificate requests.");
            parser.Setup(o => o.FriendlyName)
                .As("friendlyname")
                .WithDescription("Give the friendly name of certificate, either to be used for creating a new one or to target a command (like --cancel or --renew) at as specific one");
            parser.Setup(o => o.Cancel)
                .As("cancel")
                .WithDescription("Cancels existing scheduled renewal as specified by the target parameters.");
            parser.Setup(o => o.List)
                .As("list")
                .WithDescription("List all created renewals.");

            // Target
            parser.Setup(o => o.Target)
                .As("target")
                .WithDescription("Specify which target plugin to run, bypassing the main menu and triggering unattended mode.");
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
            parser.Setup(o => o.ManualTargetIsIIS)
                .As("manualtargetisiis")
                .WithDescription("[--target manual] Is the target of the manual host an IIS website?");

            // Validation
            parser.Setup(o => o.Validation)
                .As("validation")
                .WithDescription("Specify which validation plugin to run. If none is specified, FileSystem validation will be chosen as the default.");
            parser.Setup(o => o.ValidationMode)
                .As("validationmode")
                .SetDefault(Constants.Http01ChallengeType)
                .WithDescription("Specify which validation mode to use.");
            parser.Setup(o => o.WebRoot)
                .As("webroot")
                .WithDescription("[--validationmode http-01 --validation filesystem] A web root for the manual host name for validation.");
            parser.Setup(o => o.ValidationPort)
                .As("validationport")
                .WithDescription("[--validationmode http-01 --validation selfhosting] Port to use for listening to http-01 validation requests. Defaults to 80.");
            parser.Setup(o => o.ValidationSiteId)
                .As("validationsiteid")
                .WithDescription("[--validationmode http-01 --validation filesystem|iis] Specify site to use for handling validation requests. Defaults to --siteid.");
            parser.Setup(o => o.Warmup)
                .As("warmup")
                .WithDescription("[--validationmode http-01] Warm up websites before attempting HTTP authorization.");
            parser.Setup(o => o.UserName)
                .As("username")
                .WithDescription("[--validationmode http-01 --validation ftp|sftp|webdav] Username for ftp(s)/WebDav server.");
            parser.Setup(o => o.Password)
                .As("password")
                .WithDescription("[--validationmode http-01 --validation ftp|sftp|webdav] Password for ftp(s)/WebDav server.");
            parser.Setup(o => o.DnsCreateScript)
                .As("dnscreatescript")
                .WithDescription("[--validationmode dns-01 --validation dnsscript] Path to script to create TXT record. Parameters passed are the host name, record name and desired content.");
            parser.Setup(o => o.DnsDeleteScript)
                .As("dnsdeletescript")
                .WithDescription("[--validationmode dns-01 --validation dnsscript] Path to script to remove TXT record. Parameters passed are the host name and record name.");

            // Store
            parser.Setup(o => o.Store)
                .As("store")
                .WithDescription("Specify which store plugin to use.");
            parser.Setup(o => o.KeepExisting)
                .As("keepexisting")
                .WithDescription("While renewing, do not remove the previous certificate.");
            parser.Setup(o => o.CentralSslStore)
                .As("centralsslstore")
                .WithDescription("[--store centralssl] When using this setting, certificate files are stored to the CCS and IIS bindings are configured to reflect that.");
            parser.Setup(o => o.PfxPassword)
                .As("pfxpassword")
                .WithDescription("[--store centralssl] Password to set for .pfx files exported to the IIS CSS.");
            parser.Setup(o => o.CertificateStore)
                .As("certificatestore")
                .WithDescription("[--store certificatestore] This setting can be used to target a specific Certificate Store for a renewal.");

            // Installation
            parser.Setup(o => o.Installation)
                .As("installation")
                .WithDescription("Specify which installation plugins to use. This may be a comma separated list.");

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

        public override bool Validate(ILogService log, MainArguments result, MainArguments main)
        {
            if (result.Renew)
            {
                if (
                    !string.IsNullOrEmpty(result.CentralSslStore) ||
                    !string.IsNullOrEmpty(result.CertificateStore) ||
                    !string.IsNullOrEmpty(result.CommonName) ||
                    !string.IsNullOrEmpty(result.DnsCreateScript) ||
                    !string.IsNullOrEmpty(result.DnsDeleteScript) ||
                    !string.IsNullOrEmpty(result.ExcludeBindings) ||
                    !string.IsNullOrEmpty(result.FriendlyName) ||
                    !string.IsNullOrEmpty(result.Host) ||
                    !string.IsNullOrEmpty(result.Installation) ||
                    result.KeepExisting ||
                    result.ManualTargetIsIIS ||
                    !string.IsNullOrEmpty(result.Password) ||
                    !string.IsNullOrEmpty(result.PfxPassword) ||
                    !string.IsNullOrEmpty(result.SiteId) ||
                    !string.IsNullOrEmpty(result.Store) ||
                    !string.IsNullOrEmpty(result.Target) ||
                    !string.IsNullOrEmpty(result.UserName) ||
                    !string.IsNullOrEmpty(result.Validation) ||
                    result.ValidationPort != null ||
                    !string.IsNullOrEmpty(result.ValidationSiteId) ||
                    result.Warmup ||
                    !string.IsNullOrEmpty(result.WebRoot)
                )
                {
                    log.Error("It's not possible to change properties during renewal. Edit the .json files or overwrite the renewal if you wish to change any settings.");
                    return false;
                }
            }
            return true;
        }
    }
}