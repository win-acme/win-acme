using Autofac;
using Microsoft.Win32;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Ocsp;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Host.Services.Legacy;
using PKISharp.WACS.Plugins;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.ValidationPlugins;
using PKISharp.WACS.Services.Legacy;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal class AutofacBuilder : IAutofacBuilder
    {
        private readonly IPluginService _plugins;

        public AutofacBuilder(IPluginService plugins) => _plugins = plugins;

        /// <summary>
        /// This is used to import renewals from 1.9.x
        /// </summary>
        /// <param name="main"></param>
        /// <param name="fromUri"></param>
        /// <param name="toUri"></param>
        /// <returns></returns>
        public ILifetimeScope Legacy(ILifetimeScope main, Uri fromUri, Uri toUri)
        {
            return main.BeginLifetimeScope(builder =>
            {
                var realSettings = main.Resolve<ISettingsService>();
                var realArguments = main.Resolve<MainArguments>();
   
                builder.Register(c => new MainArguments { 
                        BaseUri = fromUri.ToString()
                    }).
                    As<MainArguments>().
                    SingleInstance();

                builder.RegisterType<LegacySettingsService>().
                    WithParameter(new TypedParameter(typeof(ISettingsService), realSettings)).
                    SingleInstance();

                builder.RegisterType<LegacyTaskSchedulerService>();

                builder.RegisterType<TaskSchedulerService>().
                    WithParameter(new TypedParameter(typeof(MainArguments), realArguments)).
                    WithParameter(new TypedParameter(typeof(ISettingsService), realSettings)).
                    SingleInstance();

                builder.Register((scope) => main.Resolve<IRenewalStore>()).
                    As<IRenewalStore>().
                    SingleInstance();

                // Check where to load Renewals from
                var hive = Registry.CurrentUser;
                var key = hive.OpenSubKey($"Software\\letsencrypt-win-simple");
                if (key == null)
                {
                    hive = Registry.LocalMachine;
                    key = hive.OpenSubKey($"Software\\letsencrypt-win-simple");
                }
                var log = main.Resolve<ILogService>();
                if (key != null)
                {
                    log.Debug("Loading legacy renewals from registry hive {name}", hive.Name);
                    builder.RegisterType<RegistryLegacyRenewalService>().
                            As<ILegacyRenewalService>().
                            WithParameter(new NamedParameter("hive", hive.Name)).
                            SingleInstance();
                }
                else
                {
                    log.Debug("Loading legacy renewals from file");
                    builder.RegisterType<FileLegacyRenewalService>().
                        As<ILegacyRenewalService>().
                        SingleInstance();
                }

                builder.RegisterType<Importer>().
                    SingleInstance();
            });
        }

        /// <summary>
        /// For configuration and renewal
        /// </summary>
        /// <param name="main"></param>
        /// <param name="renewal"></param>
        /// <returns></returns>
        public async Task<ILifetimeScope> Target(ILifetimeScope parent, Renewal renewal)
        {
            var options = renewal.TargetPluginOptions;
            if (options == null ||
                options.GetType() == typeof(TargetPluginOptions))
            {
                return parent;
            }
            if (!_plugins.TryGetPlugin(options, out var plugin)) 
            {
                return parent;
            }
            var intermediateScope = parent.BeginLifetimeScope("target-inner", builder =>
            {
                builder.RegisterInstance(options).As<TargetPluginOptions>().As(options.GetType());
                builder.RegisterType(plugin.Runner).As<ITargetPlugin>();
                builder.RegisterInstance(plugin).Keyed<Plugin>("target");
            });
            var pluginService = parent.Resolve<IPluginService>();
            var targetPlugin = intermediateScope.Resolve<ITargetPlugin>();
            var initialTarget = await targetPlugin.Generate();
            if (initialTarget != null)
            {
                return intermediateScope.BeginLifetimeScope("target-outer", builder => builder.RegisterInstance(initialTarget));
            }
            else
            {
                return intermediateScope;
            }         
        }

        /// <summary>
        /// For renewal and creating scheduled task 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public ILifetimeScope Execution(ILifetimeScope target, Renewal renewal, RunLevel runLevel)
        {
            return target.BeginLifetimeScope("execution", builder =>
            {
                builder.Register(c => runLevel).As<RunLevel>();
                builder.RegisterType<FindPrivateKey>().SingleInstance();
                builder.RegisterInstance(renewal);
                builder.RegisterInstance(renewal.StorePluginOptions).As(renewal.StorePluginOptions.GetType());
                if (renewal.CsrPluginOptions != null)
                {
                    builder.RegisterInstance(renewal.CsrPluginOptions).As(renewal.CsrPluginOptions.GetType());
                }
                if (renewal.OrderPluginOptions != null)
                {
                    builder.RegisterInstance(renewal.OrderPluginOptions).As(renewal.OrderPluginOptions.GetType());
                }
                builder.RegisterInstance(renewal.TargetPluginOptions).As(renewal.TargetPluginOptions.GetType());
                if (renewal.CsrPluginOptions != null)
                {
                    builder.RegisterType(renewal.CsrPluginOptions.Instance).As<ICsrPlugin>().SingleInstance();
                }
                if (renewal.OrderPluginOptions != null)
                {
                    builder.RegisterType(renewal.OrderPluginOptions.Instance).As<IOrderPlugin>().SingleInstance();
                }
                else
                {
                    builder.RegisterType<Plugins.OrderPlugins.Single>().As<IOrderPlugin>().SingleInstance();
                }
                builder.RegisterType(renewal.TargetPluginOptions.Instance).As<ITargetPlugin>().SingleInstance();
            });
        }

        /// <summary>
        /// For a single order within the renewal
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public ILifetimeScope Order(ILifetimeScope execution)
        {
            var renewal = execution.Resolve<Renewal>();
            return execution.BeginLifetimeScope("order", builder =>
            {
                if (renewal.CsrPluginOptions != null)
                {
                    builder.RegisterType(renewal.CsrPluginOptions.Instance).As<ICsrPlugin>().SingleInstance();
                }
            });
        }

        /// <summary>
        /// Validation
        /// </summary>
        /// <param name="execution"></param>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public ILifetimeScope Validation(ILifetimeScope execution, ValidationPluginOptions options)
        {
            return execution.BeginLifetimeScope("validation", builder =>
            {
                builder.RegisterType<HttpValidationParameters>();
                builder.RegisterInstance(options).As(options.GetType());
                builder.RegisterType(options.Instance).
                    As<IValidationPlugin>().
                    SingleInstance();
            });
        }
    }
}
