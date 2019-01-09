using Autofac;
using Nager.PublicSuffix;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;

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

            builder.Register(c => new OptionsParser(logger, pluginService, args).Options).
                As<Options>().
                SingleInstance();

            builder.RegisterType<OptionsService>().
                As<IOptionsService>().
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
            builder.RegisterType<AutofacBuilder>();
            builder.RegisterInstance(pluginService);

            return builder.Build();
        }
    }
}