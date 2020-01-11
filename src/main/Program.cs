using Autofac;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Host
{
    /// <summary>
    /// This class serves as bootstrapper to call the main library
    /// </summary>
    internal class Program
    {
        private async static Task Main(string[] args)
        {
            // Default for older versions of Windows is Tls 1.0 only, change to Tls 1.2 or 1.3
            ServicePointManager.SecurityProtocol = 
                SecurityProtocolType.Tls12 | 
                SecurityProtocolType.Tls13;

            // Error handling
            AppDomain.CurrentDomain.UnhandledException += 
                new UnhandledExceptionEventHandler(OnUnhandledException);

            // Uncomment to debug with a local proxy like Fiddler
            // System.Net.ServicePointManager.ServerCertificateValidationCallback += 
            //    (sender, cert, chain, sslPolicyErrors) => true;

            // Setup IOC container
            var container = GlobalScope(args);
            if (container == null)
            {
                Environment.ExitCode = -1;
                return;
            }

            // The main class might change the character encoding
            // save the original setting so that it can be restored
            // after the run.
            var original = Console.OutputEncoding;

            try
            {           
                // Load instance of the main class and start the program
                var wacs = new Wacs(container);
                Environment.ExitCode = await wacs.Start();
            } 
            catch (Exception ex)
            {
                Console.WriteLine("Error in main function: " + ex.Message);
                Environment.ExitCode = -1;
            }

            // Restore original code page
            Console.OutputEncoding = original;
        }

        /// <summary>
        /// Final resort to catch unhandled exceptions and log something
        /// before the runtime explodes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Console.WriteLine("Unhandled exception caught: " + ex.Message);
        }

        /// <summary>
        /// Configure dependency injection 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
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
            if (!argumentsService.Valid)
            {
                return null;
            }
            var settingsService = new SettingsService(logger, argumentsService);
            if (!settingsService.Valid)
            {
                return null;
            }
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
            _ = builder.RegisterType<IISHelper>().SingleInstance();
            _ = builder.RegisterType<ExceptionHandler>().SingleInstance();
            _ = builder.RegisterType<UnattendedResolver>();
            _ = builder.RegisterType<InteractiveResolver>();
            _ = builder.RegisterType<AutofacBuilder>().As<IAutofacBuilder>().SingleInstance();
            _ = builder.RegisterType<AcmeClient>().SingleInstance();
            _ = builder.RegisterType<PemService>().SingleInstance();
            _ = builder.RegisterType<EmailClient>().SingleInstance();
            _ = builder.RegisterType<ScriptClient>().SingleInstance();
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