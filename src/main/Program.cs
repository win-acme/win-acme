using Autofac;
using Nager.PublicSuffix;
using PKISharp.WACS.Acme;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;
using System.Diagnostics;

namespace PKISharp.WACS
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
            var originalCodePage = Console.OutputEncoding.CodePage;
            SetCodePage(65001);
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Load main instance
            var wacs = new Wacs(container);
            wacs.Start();

            // Restore original code page
            SetCodePage(originalCodePage);
        }

        internal static void SetCodePage(int codePage)
        {
            var cmd = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            cmd.Start();
            cmd.StandardInput.WriteLine($"chcp {codePage}");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
        }

        internal static IContainer GlobalScope(string[] args)
        {
            var builder = new ContainerBuilder();
            var logger = new LogService();
            var pluginService = new PluginService(logger);

            builder.RegisterInstance(logger).
                As<ILogService>().
                SingleInstance();

            builder.RegisterType<ArgumentsParser>().
                SingleInstance().
                WithParameter(new TypedParameter(typeof(string[]), args));

            builder.RegisterType<ArgumentsService>().
                As<IArgumentsService>().
                SingleInstance();

            builder.RegisterType<SettingsService>().
                As<ISettingsService>().
                SingleInstance();

            builder.RegisterType<InputService>().
                As<IInputService>().
                SingleInstance();

            builder.RegisterType<ProxyService>().
                SingleInstance();

            builder.RegisterType<PasswordGenerator>().
                SingleInstance();

            builder.RegisterType<RenewalService>().
               As<IRenewalService>().
               SingleInstance();

            builder.RegisterType<DotNetVersionService>().
                SingleInstance();

            pluginService.Configure(builder);

            builder.Register(c => new DomainParser(new WebTldRuleProvider())).SingleInstance();
            builder.RegisterType<IISClient>().As<IIISClient>().InstancePerLifetimeScope();
            builder.RegisterType<IISBindingHelper>().SingleInstance();
            builder.RegisterType<IISSiteHelper>().SingleInstance();
            builder.RegisterType<UnattendedResolver>();
            builder.RegisterType<InteractiveResolver>();
            builder.RegisterType<AutofacBuilder>().SingleInstance();
            builder.RegisterType<AcmeClient>().SingleInstance();
            builder.RegisterType<PemService>().SingleInstance();
            builder.RegisterType<EmailClient>().SingleInstance();
            builder.RegisterType<LookupClientProvider>().SingleInstance();
            builder.RegisterType<CertificateService>().As<ICertificateService>().SingleInstance();
            builder.RegisterType<TaskSchedulerService>().SingleInstance();
            builder.RegisterType<NotificationService>().SingleInstance();
            builder.RegisterInstance(pluginService);

            return builder.Build();
        }
    }
}