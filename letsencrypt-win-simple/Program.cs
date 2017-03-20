using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using CommandLine;
using Serilog;
using LetsEncrypt.ACME.Simple.Core.Configuration;
using LetsEncrypt.ACME.Simple.Core.Interfaces;
using LetsEncrypt.ACME.Simple.Core.Plugins;
using LetsEncrypt.ACME.Simple.Core.Services;

namespace LetsEncrypt.ACME.Simple
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

            // Compose objects
            var options = TryParseOptions(args);
            if(options == null)
                return;

            var app = new Setup();
            app.Initialize(options);
            var consoleService = new ConsoleService(options);
            var certificateService = new CertificateService(options, consoleService);
            var letsEncryptService = new LetsEncryptService(options, certificateService, consoleService);
            var pluginService = new PluginService(options, certificateService, letsEncryptService, consoleService);
            options.Plugins = GetPlugins(options, certificateService, letsEncryptService, consoleService, pluginService);
            var acmeClientService = new AcmeClientService(options, certificateService, consoleService);
            var appService = new AppService(options, certificateService, letsEncryptService, consoleService, acmeClientService);

            // The app can now actually start
            appService.LaunchApp();
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

        private static Dictionary<string, Plugin> GetPlugins(IOptions options, ICertificateService certificateService, 
            ILetsEncryptService letsEncryptService, IConsoleService consoleService, IPluginService pluginService)
        {
            var plugins = new Dictionary<string, Plugin>();
            try
            {
                // find class libraries with plugins in them
                var currentPath = AppDomain.CurrentDomain.BaseDirectory;
                string[] extensions = { ".dll", ".exe" };
                foreach (var file in Directory.EnumerateFiles(currentPath, "*.*")
                    .Where(x => extensions.Any(ext => ext == Path.GetExtension(x))))
                {
                    var assembly = Assembly.LoadFile(file);
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.BaseType == null || type.BaseType != typeof(Plugin))
                            continue;

                        try
                        {
                            var ctor = type.GetConstructor(new[]
                            {
                                typeof(IOptions), typeof(ICertificateService),
                                typeof(ILetsEncryptService), typeof(IConsoleService),
                                typeof(IPluginService)
                            });

                            var plugin = ctor.Invoke(new object[]
                            {
                                options, certificateService,
                                letsEncryptService, consoleService,
                                pluginService
                            }) as Plugin;

                            plugins.Add(plugin.Name, plugin);                                
                        }
                        catch (Exception e)
                        {
                            Log.Error("Error adding plugin {error}", e);
                        }
                    }
                    assembly = null;
                }
            }
            catch
            {
                // ignore errors loading assemblies
            }
            return plugins;
        }
    }
}
