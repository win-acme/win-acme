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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PKISharp.WACS.Host
{
    /// <summary>
    /// This class serves as bootstrapper to call the main library
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Prevent starting program twice at the same time
        /// </summary>
        private static Mutex _localMutex;
        private static Mutex _globalMutex;

        private static bool Verbose { get; set; }

        private async static Task Main(string[] args)
        {
            // Error handling
            AppDomain.CurrentDomain.UnhandledException += 
                new UnhandledExceptionEventHandler(OnUnhandledException);

            // Are we running in verbose mode?
            Verbose = args.Contains("--verbose");

            // Setup IOC container
            var container = GlobalScope(args);
            if (container == null)
            {
                FriendlyClose();
                return;
            }

            // Check for multiple instances
            if (!AllowInstanceToRun(container))
            {
                FriendlyClose();
                return;
            }

            // The main class might change the character encoding
            // save the original setting so that it can be restored
            // after the run.
            var original = Console.OutputEncoding;

            try
            {
                // Load instance of the main class and start the program
                var wacs = container.Resolve<Wacs>(new TypedParameter(typeof(IContainer), container));
                Environment.ExitCode = await wacs.Start();
            } 
            catch (Exception ex)
            {
                Console.WriteLine(" Error in main function: " + ex.Message);
                if (Verbose)
                {
                    Console.WriteLine(ex.StackTrace);
                }
                FriendlyClose();
            }

            // Restore original code page
            Console.OutputEncoding = original;
        }

        /// <summary>
        /// Block multiple instances from running at the same time
        /// on the same configuration path, because they might 
        /// overwrite eachothers stuff
        /// </summary>
        /// <returns></returns>
        static bool AllowInstanceToRun(IContainer container)
        {
            var logger = container.Resolve<ILogService>();
            var settings = container.Resolve<ISettingsService>();
            var globalKey = "wacs.exe";
            var localKey = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes($"{globalKey}-{settings.Client.ConfigurationPath}")));
            _localMutex = new Mutex(true, localKey, out var created);
            if (!created)
            {
                logger.Error("Another instance of wacs.exe is already working in {path}. This instance will now close to protect the integrity of the configuration.", settings.Client.ConfigurationPath);
                return false;
            }
            _globalMutex = new Mutex(true, globalKey, out created);
            if (!created)
            {
                logger.Warning("Another instance of wacs.exe is already running, it is not recommended to run multiple instances simultaneously.");
            }
            return true;
        }

        /// <summary>
        /// Close in a friendly way
        /// </summary>
        static void FriendlyClose()
        {
            Environment.ExitCode = -1;
            if (Environment.UserInteractive)
            {
                Console.WriteLine(" Press <Enter> to close");
                _ = Console.ReadLine();
            }
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
            Console.WriteLine(" Unhandled exception caught: " + ex.Message);
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
            if (Verbose)
            {
                logger.SetVerbose();
            }
            // Not used but should be called anyway because it 
            // checks if we're not running as dotnet.exe and also
            // prints some verbose messages that are interesting
            // to know very early in the start up process
            var versionService = new VersionService(logger);
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
            _ = builder.RegisterType<UserRoleService>().As<IUserRoleService>().SingleInstance();
            _ = builder.RegisterType<InputService>().As<IInputService>().SingleInstance();
            _ = builder.RegisterType<ProxyService>().SingleInstance();
            _ = builder.RegisterType<PasswordGenerator>().SingleInstance();
            _ = builder.RegisterType<RenewalStoreDisk>().As<IRenewalStore>().SingleInstance();

            pluginService.Configure(builder);

            _ = builder.RegisterType<DomainParseService>().SingleInstance();
            _ = builder.RegisterType<IISClient>().As<IIISClient>().InstancePerLifetimeScope();
            _ = builder.RegisterType<IISHelper>().SingleInstance();
            _ = builder.RegisterType<ExceptionHandler>().SingleInstance();
            _ = builder.RegisterType<UnattendedResolver>();
            _ = builder.RegisterType<InteractiveResolver>();
            _ = builder.RegisterType<AutofacBuilder>().As<IAutofacBuilder>().SingleInstance();
            _ = builder.RegisterType<AcmeClient>().SingleInstance();
            _ = builder.RegisterType<ZeroSsl>().SingleInstance();
            _ = builder.RegisterType<OrderManager>().SingleInstance();
            _ = builder.RegisterType<PemService>().SingleInstance();
            _ = builder.RegisterType<EmailClient>().SingleInstance();
            _ = builder.RegisterType<ScriptClient>().SingleInstance();
            _ = builder.RegisterType<LookupClientProvider>().SingleInstance();
            _ = builder.RegisterType<CertificateService>().As<ICertificateService>().SingleInstance();
            _ = builder.RegisterType<TaskSchedulerService>().SingleInstance();
            _ = builder.RegisterType<NotificationService>().SingleInstance();
            _ = builder.RegisterType<RenewalExecutor>().SingleInstance();
            _ = builder.RegisterType<RenewalValidator>().SingleInstance();
            _ = builder.RegisterType<RenewalManager>().SingleInstance();
            _ = builder.RegisterType<RenewalCreator>().SingleInstance();
            _ = builder.Register(c => c.Resolve<IArgumentsService>().MainArguments).SingleInstance();

            _ = builder.RegisterType<Wacs>();

            return builder.Build();
        }
    }
}