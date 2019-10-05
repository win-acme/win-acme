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
using System.Net;

namespace PKISharp.WACS.Host
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Setup DI
            var container = GlobalScope(args);

            // Default is Tls 1.0 only, change to Tls 1.2 or 1.3
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            // Uncomment the follow line to test with Fiddler
            // System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            // Enable international character rendering
            var original = Console.OutputEncoding;

            // Load main instance
            new Wacs(container).Start().Wait();

            // Restore original code page
            Console.OutputEncoding = original;
        }
        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
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
            logger.SetDiskLoggingPath(settingsService.Client.LogPath);

            _ = builder.RegisterInstance(argumentsService);
            _ = builder.RegisterInstance(argumentsParser);
            _ = builder.RegisterInstance(logger).As<ILogService>();
            _ = builder.RegisterInstance(settingsService).As<ISettingsService>();
            _ = builder.RegisterInstance(argumentsService).As<IArgumentsService>();
            _ = builder.RegisterInstance(pluginService).As<IPluginService>();
            _ = builder.RegisterType<UserRoleService>().SingleInstance();
            _ = builder.RegisterType<InputService>().As<IInputService>().SingleInstance();
            _ = builder.RegisterType<ProxyService>().SingleInstance();
            _ = builder.RegisterType<PasswordGenerator>().SingleInstance();
            _ = builder.RegisterType<RenewalService>().As<IRenewalStore>().SingleInstance();

            pluginService.Configure(builder);

            _ = builder.RegisterType<DomainParseService>().SingleInstance();
            _ = builder.RegisterType<IISClient>().As<IIISClient>().SingleInstance();
            _ = builder.RegisterType<IISBindingHelper>().SingleInstance();
            _ = builder.RegisterType<IISSiteHelper>().SingleInstance();
            _ = builder.RegisterType<ExceptionHandler>().SingleInstance();
            _ = builder.RegisterType<UnattendedResolver>();
            _ = builder.RegisterType<InteractiveResolver>();
            _ = builder.RegisterType<AutofacBuilder>().As<IAutofacBuilder>().SingleInstance();
            _ = builder.RegisterType<AcmeClient>().SingleInstance();
            _ = builder.RegisterType<PemService>().SingleInstance();
            _ = builder.RegisterType<EmailClient>().SingleInstance();
            _ = builder.RegisterType<LookupClientProvider>().SingleInstance();
            _ = builder.RegisterType<CertificateService>().As<ICertificateService>().SingleInstance();
            _ = builder.RegisterType<TaskSchedulerService>().SingleInstance();
            _ = builder.RegisterType<NotificationService>().SingleInstance();
            _ = builder.RegisterType<RenewalExecutor>().SingleInstance();
            _ = builder.RegisterType<RenewalManager>().SingleInstance();
            _ = builder.Register(c => c.Resolve<IArgumentsService>().MainArguments).SingleInstance();

            return builder.Build();
        }
    }
}