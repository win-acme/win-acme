using Autofac;
using PKISharp.WACS.Acme;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;
using System.Linq;

namespace PKISharp.WACS.Host
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Setup DI
            var container = GlobalScope(args);

            // .NET Framework check
            var dn = container.Resolve<DotNetVersionService>();
            if (!dn.Check())
            {
                return;
            }

            // Default is Tls 1.0 only, change to Tls 1.2 only
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Uncomment the follow line to test with Fiddler
            // System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            // Enable international character rendering
            var original = Console.OutputEncoding;
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            // Load main instance
            var wacs = new Wacs(container);
            wacs.Start();

            // Restore original code page
            Console.OutputEncoding = original;
        }

        internal static IContainer GlobalScope(string[] args)
        {
            var builder = new ContainerBuilder();
            var logger = new LogService();
            if (args.Contains("--verbose"))
            {
                logger.SetVerbose();
            }
            var pluginService = new PluginService(logger);
            var argumentsParser = new ArgumentsParser(logger, pluginService, args);
            var argumentsService = new ArgumentsService(logger, argumentsParser);
            var settingsService = new SettingsService(logger, argumentsService);
            logger.SetDiskLoggingPath(settingsService.LogPath);

            builder.RegisterInstance(argumentsService);
            builder.RegisterInstance(argumentsParser);
            builder.RegisterInstance(logger).As<ILogService>();
            builder.RegisterInstance(settingsService).As<ISettingsService>();
            builder.RegisterInstance(argumentsService).As<IArgumentsService>();
            builder.RegisterInstance(pluginService);

            builder.RegisterType<InputService>().
                As<IInputService>().
                SingleInstance();

            builder.RegisterType<ProxyService>().
                SingleInstance();

            builder.RegisterType<PasswordGenerator>().
                SingleInstance();

            builder.RegisterType<RenewalService>().
               As<IRenewalStore>().
               SingleInstance();

            builder.RegisterType<DotNetVersionService>().
                SingleInstance();

            pluginService.Configure(builder);

            builder.RegisterType<DomainParseService>().SingleInstance();
            builder.RegisterType<IISClient>().As<IIISClient>().SingleInstance();
            builder.RegisterType<IISBindingHelper>().SingleInstance();
            builder.RegisterType<IISSiteHelper>().SingleInstance();
            builder.RegisterType<ExceptionHandler>().SingleInstance();
            builder.RegisterType<UnattendedResolver>();
            builder.RegisterType<InteractiveResolver>();
            builder.RegisterType<AutofacBuilder>().As<IAutofacBuilder>().SingleInstance();
            builder.RegisterType<AcmeClient>().SingleInstance();
            builder.RegisterType<PemService>().SingleInstance();
            builder.RegisterType<EmailClient>().SingleInstance();
            builder.RegisterType<LookupClientProvider>().SingleInstance();
            builder.RegisterType<CertificateService>().As<ICertificateService>().SingleInstance();
            builder.RegisterType<TaskSchedulerService>().SingleInstance();
            builder.RegisterType<NotificationService>().SingleInstance();
            builder.RegisterType<RenewalExecutor>().SingleInstance();
            builder.RegisterType<RenewalManager>().SingleInstance();
            builder.Register(c => c.Resolve<IArgumentsService>().MainArguments).SingleInstance();
            return builder.Build();
        }
    }
}