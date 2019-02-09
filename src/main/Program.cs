using Autofac;
using DnsClient;
using Nager.PublicSuffix;
using PKISharp.WACS.Acme;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;
using System.Linq;

namespace PKISharp.WACS
{
    internal class Program
    {
        private static IContainer _container;

        private static void Main(string[] args)
        {
            // Setup DI
            _container = GlobalScope(args);

            // .NET Framework check
            var dn = _container.Resolve<DotNetVersionService>();
            if (!dn.Check())
            {
                return;
            }

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Uncomment to test with Fiddler
            // System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Load main instance
            var wacs = new Wacs(_container);
            wacs.Start();
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
            builder.RegisterType<IISClient>().As<IIISClient>().SingleInstance();
            builder.RegisterType<IISBindingHelper>().SingleInstance();
            builder.RegisterType<IISSiteHelper>().SingleInstance();
            builder.RegisterType<UnattendedResolver>();
            builder.RegisterType<InteractiveResolver>();
            builder.RegisterType<AutofacBuilder>().SingleInstance();
            builder.RegisterType<AcmeClient>().SingleInstance();
            builder.RegisterType<EmailClient>().SingleInstance();
            builder.RegisterInstance(pluginService);

            return builder.Build();
        }
    }
}