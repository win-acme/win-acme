using Autofac;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Renewal;
using Microsoft.Win32;
using Nager.PublicSuffix;
using System.Collections.Generic;
using System.Linq;
using PKISharp.WACS.Acme;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services.Renewal.Legacy;

namespace PKISharp.WACS
{
    internal class AutofacBuilder
    {
        internal static IContainer Global(string[] args)
        {
            var builder = new ContainerBuilder();

            var logger = new LogService();
            builder.RegisterInstance(logger);

            builder.RegisterType<LogService>().
                As<ILogService>().
                SingleInstance();

            builder.RegisterType<OptionsService>().
                As<IOptionsService>().
                WithParameter(new TypedParameter(typeof(string[]), args)).
                SingleInstance();

            builder.RegisterType<SettingsService>().
                SingleInstance();

            builder.RegisterType<InputService>().
                As<IInputService>().
                SingleInstance();

            builder.RegisterType<ProxyService>().
                SingleInstance();

            builder.RegisterType<FileRenewalService>().
               As<IRenewalService>().
               SingleInstance();

            builder.RegisterType<DotNetVersionService>().
                SingleInstance();

            var pluginService = new PluginService(logger);
            pluginService.Configure(builder);

            builder.Register(c => new DomainParser(new WebTldRuleProvider())).SingleInstance();
            builder.RegisterType<IISClient>().SingleInstance();
            builder.RegisterType<UnattendedResolver>();
            builder.RegisterType<InteractiveResolver>();
            builder.RegisterInstance(pluginService);

            return builder.Build();
        }

        internal static ILifetimeScope Renewal(ILifetimeScope main, ScheduledRenewal renewal, RunLevel runLevel)
        {
            IResolver resolver = null;
            if (runLevel > RunLevel.Unattended)
            {
                resolver = main.Resolve<InteractiveResolver>(
                    new TypedParameter(typeof(ScheduledRenewal), renewal), 
                    new TypedParameter(typeof(RunLevel), runLevel));
            }
            else
            {
                resolver = main.Resolve<UnattendedResolver>(new TypedParameter(typeof(ScheduledRenewal), renewal));
            }
            return main.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<AcmeClient>().SingleInstance();
                builder.RegisterType<CertificateService>().SingleInstance();

                builder.RegisterInstance(resolver);
                if (renewal != null)
                {
                    builder.RegisterInstance(renewal);
                }
                builder.Register(c => runLevel).As<RunLevel>();

                builder.RegisterType<TaskSchedulerService>().
                    WithParameter(new TypedParameter(typeof(RunLevel), runLevel)).
                    SingleInstance();

                builder.Register(c => resolver.GetTargetPlugin(main)).As<ITargetPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetInstallationPlugins(main)).As<List<IInstallationPluginFactory>>().SingleInstance();
                builder.Register(c => resolver.GetValidationPlugin(main)).As<IValidationPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetStorePlugin(main)).As<IStorePluginFactory>().SingleInstance();

                builder.Register(c => c.Resolve(c.Resolve<ITargetPluginFactory>().Instance)).As<ITargetPlugin>().SingleInstance();
                builder.Register(c => c.Resolve(c.Resolve<IStorePluginFactory>().Instance)).As<IStorePlugin>().SingleInstance();
            });
        }

        internal static ILifetimeScope Identifier(ILifetimeScope renewalScope, Target target, string identifier)
        {
            return renewalScope.BeginLifetimeScope(builder =>
            {
                builder.Register(c => c.Resolve(
                    c.Resolve<IValidationPluginFactory>().Instance, 
                    new TypedParameter(typeof(string), identifier), 
                    new TypedParameter(typeof(Target), target))).As<IValidationPlugin>().SingleInstance();
            });
        }
    }
}
