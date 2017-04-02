using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using Autofac;
using CommandLine;
using LetsEncryptWinSimple.Core.Configuration;
using LetsEncryptWinSimple.Core.Extensions;
using LetsEncryptWinSimple.Core.Interfaces;
using LetsEncryptWinSimple.Core.Services;
using Serilog;

namespace LetsEncryptWinSimple.Cli
{
    internal class Program
    {
        static bool IsElevated
            => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        private static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if (IsNet45OrNewer() == false)
            {
                Log.Error("Error: You need to install .NET framework 4.5 on this machine in order to be able to run this app");
                return;
            }

            var options = TryParseOptions(args);
            if (options == null)
                return;

            var app = new Setup();
            app.Initialize(options);

            var builder = new ContainerBuilder();
            builder.Register(ctx => options).As<IOptions>();
            builder.RegisterType<ConsoleService>().As<IConsoleService>();
            builder.RegisterType<CertificateService>().As<ICertificateService>();
            builder.RegisterType<LetsEncryptService>().As<ILetsEncryptService>();
            builder.RegisterType<PluginService>().As<IPluginService>();
            builder.RegisterType<AcmeClientService>().As<IAcmeClientService>();
            builder.RegisterType<AppService>().As<IAppService>();

            RegisterPlugins(builder);
            
            var container = builder.Build();
            using (var scope = container.BeginLifetimeScope())
            {
                var appService = scope.Resolve<IAppService>();

                //Get all plugins and set Options.Plugins
                var resolvedOptions = scope.Resolve<IOptions>();
                resolvedOptions.Plugins = scope.GetImplementingTypes<IPlugin>();

                // The app can now actually start
                appService.LaunchApp();
            }
        }

        // From: http://stackoverflow.com/a/8543850/5018
        public static bool IsNet45OrNewer()
        {
            // Class "ReflectionContext" exists from .NET 4.5 onwards.
            return Type.GetType("System.Reflection.ReflectionContext", false) != null;
        }

        public static Options TryParseOptions(string[] args)
        {
            try
            {
                var commandLineParseResult = Parser.Default.ParseArguments<Options>(args);
                var parsed = commandLineParseResult as Parsed<Options>;
                if (parsed == null)
                    return null; // not parsed - usually means `--help` has been passed in

                var options = parsed.Value;

                Log.Debug("{@Options}", options);

                return options;
            }
            catch (Exception e)
            {
                Log.Error("Failed while parsing options.", e);
                throw;
            }
        }

        private static void RegisterPlugins(ContainerBuilder builder)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] extensions = {".dll", ".exe"};
            var allFiles = Directory.EnumerateFileSystemEntries(baseDir, "*.*")
                .Where(x => extensions.Any(ext => ext == Path.GetExtension(x)));

            var assemblies = new List<Assembly>();

            foreach (var file in allFiles)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(file);
                    var assembly = Assembly.Load(assemblyName);
                    assemblies.Add(assembly);
                }
                catch (Exception e)
                {
                    Log.Debug(e, $"Couldn't load assembly from file {file}");
                }
            }

            builder.RegisterAssemblyTypes(assemblies.ToArray())
                .Where(t => t.IsAssignableTo<IPlugin>())
                .Where(t => !t.IsAbstract)
                .AsImplementedInterfaces()
                .As<IPlugin>()
                .AsSelf()
                .InstancePerDependency();
        }
    }
}
