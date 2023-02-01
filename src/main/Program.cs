using Autofac;
using Autofac.Core;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Linq;
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
        private static Mutex? _globalMutex;

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

            // The main class might change the character encoding
            // save the original setting so that it can be restored
            // after the run.
            var original = Console.OutputEncoding;
            try
            {
                // Load instance of the main class and start the program
                AllowInstanceToRun(container);
                var wacs = container.Resolve<Wacs>();
                Environment.ExitCode = await wacs.Start().ConfigureAwait(false);
            } 
            catch (Exception ex)
            {
                Console.WriteLine(" Error in main function: " + ex.Message);
                if (Verbose)
                {
                    Console.WriteLine(ex.StackTrace);
                    while (ex.InnerException != null) {
                        ex = ex.InnerException;
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                FriendlyClose();
            } 
            finally
            {
                // Restore original code page
                Console.OutputEncoding = original;
                _globalMutex?.Dispose();
            }
        }

        /// <summary>
        /// Block multiple instances from running at the same time
        /// on the same configuration path, because they might 
        /// overwrite eachothers stuff
        /// </summary>
        /// <returns></returns>
        static void AllowInstanceToRun(ILifetimeScope container)
        {
            var logger = container.Resolve<ILogService>();
            _globalMutex = new Mutex(true, "wacs.exe", out var created);
            if (!created)
            {
                logger.Warning("Another instance of wacs.exe is already running, waiting for that to close...");
                try
                {
                    _ = _globalMutex.WaitOne();
                } 
                catch (AbandonedMutexException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Close in a friendly way
        /// </summary>
        static void FriendlyClose()
        {
            _globalMutex?.ReleaseMutex();
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
        internal static ILifetimeScope GlobalScope(string[] args)
        {
            var builder = new ContainerBuilder();

            // Single instance types
            _ = builder.RegisterType<LogService>().WithParameter(new TypedParameter(typeof(bool), Verbose)).SingleInstance().As<ILogService>();
            _ = builder.RegisterType<PluginService>().SingleInstance().As<IPluginService>();
            _ = builder.RegisterType<ArgumentsParser>().WithParameter(new TypedParameter(typeof(string[]), args)).SingleInstance();
            _ = builder.RegisterType<AdminService>().SingleInstance();
            _ = builder.RegisterType<VersionService>().SingleInstance();
            _ = builder.RegisterType<AssemblyService>().SingleInstance();
            _ = builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
            _ = builder.RegisterType<UserRoleService>().As<IUserRoleService>().SingleInstance();
            _ = builder.RegisterType<ValidationOptionsService>().As<IValidationOptionsService>().As<ValidationOptionsService>().SingleInstance();
            _ = builder.RegisterType<InputService>().As<IInputService>().SingleInstance();
            _ = builder.RegisterType<ProxyService>().As<IProxyService>().SingleInstance();
            _ = builder.RegisterType<UpdateClient>().SingleInstance();
            _ = builder.RegisterType<PasswordGenerator>().SingleInstance();
            _ = builder.RegisterType<RenewalStoreDisk>().As<IRenewalStoreBackend>().SingleInstance();
            _ = builder.RegisterType<RenewalStore>().As<IRenewalStore>().SingleInstance();
            _ = builder.RegisterType<DomainParseService>().SingleInstance();
            _ = builder.RegisterType<IISClient>().As<IIISClient>().InstancePerLifetimeScope();
            _ = builder.RegisterType<IISHelper>().SingleInstance();
            _ = builder.RegisterType<ExceptionHandler>().SingleInstance();
            _ = builder.RegisterType<AutofacBuilder>().As<IAutofacBuilder>().SingleInstance();
            _ = builder.RegisterType<AccountManager>().SingleInstance();
            _ = builder.RegisterType<AcmeClient>().SingleInstance();
            _ = builder.RegisterType<ZeroSsl>().SingleInstance();
            _ = builder.RegisterType<OrderManager>().SingleInstance();
            _ = builder.RegisterType<PemService>().SingleInstance();
            _ = builder.RegisterType<EmailClient>().SingleInstance();
            _ = builder.RegisterType<ScriptClient>().SingleInstance();
            _ = builder.RegisterType<LookupClientProvider>().SingleInstance();
            _ = builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
            _ = builder.RegisterType<CertificatePicker>().SingleInstance();
            _ = builder.RegisterType<CertificateService>().As<ICertificateService>().SingleInstance();
            _ = builder.RegisterType<DueDateRandomService>().As<IDueDateService>().SingleInstance();
            _ = builder.RegisterType<SecretServiceManager>().SingleInstance();
            _ = builder.RegisterType<JsonSecretService>().As<ISecretService>().SingleInstance();
            _ = builder.RegisterType<TaskSchedulerService>().SingleInstance();
            _ = builder.RegisterType<NotificationService>().SingleInstance();
            _ = builder.RegisterType<RenewalExecutor>().SingleInstance();
            _ = builder.RegisterType<RenewalValidator>().SingleInstance();
            _ = builder.RegisterType<OrderProcessor>().SingleInstance();
            _ = builder.RegisterType<RenewalManager>().SingleInstance();
            _ = builder.RegisterType<RenewalCreator>().SingleInstance();
            _ = builder.RegisterType<ArgumentsInputService>().SingleInstance();

            // Multi-instance types
            _ = builder.RegisterType<Wacs>();
            _ = builder.RegisterType<UnattendedResolver>();
            _ = builder.RegisterType<InteractiveResolver>();

            // Specials
            _ = builder.Register(c => c.Resolve<ArgumentsParser>().GetArguments<MainArguments>()!);
            _ = builder.Register(c => (ISharingLifetimeScope)c.Resolve<ILifetimeScope>()).As<ISharingLifetimeScope>().ExternallyOwned();
            WacsJson.Configure(builder);

            // Child scope for the plugin service
            var root = builder.Build();
            var plugin = root.Resolve<IPluginService>();
            return root.BeginLifetimeScope("wacs", plugin.Configure);
        }
    }
}