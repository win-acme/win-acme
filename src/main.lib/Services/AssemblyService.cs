using Autofac;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace PKISharp.WACS.Services
{
    public partial class AssemblyService
    {
        private readonly List<Type> _allTypes;
        internal readonly ILogService _log;

        public AssemblyService(ILogService logger)
        {
            _log = logger;
            _allTypes = new List<Type>();
            _allTypes.AddRange(BuiltInTypes());
            _allTypes.AddRange(LoadFromDisk());
        }

        internal static List<Type> BuiltInTypes()
        {
            return new()
            {
                // Arguments
                typeof(Configuration.Arguments.MainArguments),
                typeof(Configuration.Arguments.AccountArguments),
                typeof(Configuration.Arguments.NetworkCredentialArguments),
  
                // Target plugins
                typeof(Plugins.TargetPlugins.Csr),
                typeof(Plugins.TargetPlugins.IIS), typeof(Plugins.TargetPlugins.IISArguments),
                typeof(Plugins.TargetPlugins.Manual), typeof(Plugins.TargetPlugins.ManualArguments),

                // Validation plugins
                typeof(Plugins.ValidationPlugins.HttpValidationArguments),
                typeof(Plugins.ValidationPlugins.Dns.Acme), typeof(Plugins.ValidationPlugins.Dns.AcmeArguments),
                typeof(Plugins.ValidationPlugins.Dns.Manual),
                typeof(Plugins.ValidationPlugins.Dns.Script), typeof(Plugins.ValidationPlugins.Dns.ScriptArguments),
                typeof(Plugins.ValidationPlugins.Http.FileSystem), typeof(Plugins.ValidationPlugins.Http.FileSystemArguments),
                typeof(Plugins.ValidationPlugins.Http.Ftp),
                typeof(Plugins.ValidationPlugins.Http.SelfHosting), typeof(Plugins.ValidationPlugins.Http.SelfHostingArguments),
                typeof(Plugins.ValidationPlugins.Http.Sftp),
                typeof(Plugins.ValidationPlugins.Http.WebDav),
                typeof(Plugins.ValidationPlugins.Tls.SelfHosting), typeof(Plugins.ValidationPlugins.Tls.SelfHostingArguments),

                // Order plugins
                typeof(Plugins.OrderPlugins.Domain),
                typeof(Plugins.OrderPlugins.Host),
                typeof(Plugins.OrderPlugins.Single),
                typeof(Plugins.OrderPlugins.Site),

                // CSR plugins
                typeof(Plugins.CsrPlugins.CsrArguments),
                typeof(Plugins.CsrPlugins.Ec),
                typeof(Plugins.CsrPlugins.Rsa),

                // Store plugins
                typeof(Plugins.StorePlugins.CertificateStore), typeof(Plugins.StorePlugins.CertificateStoreArguments),
                typeof(Plugins.StorePlugins.CentralSsl), typeof(Plugins.StorePlugins.CentralSslArguments),
                typeof(Plugins.StorePlugins.PemFiles), typeof(Plugins.StorePlugins.PemFilesArguments),
                typeof(Plugins.StorePlugins.PfxFile), typeof(Plugins.StorePlugins.PfxFileArguments),
                typeof(Plugins.StorePlugins.Null),

                // Installation plugins
                typeof(Plugins.InstallationPlugins.IIS), typeof(Plugins.InstallationPlugins.IISArguments),
                typeof(Plugins.InstallationPlugins.Script), typeof(Plugins.InstallationPlugins.ScriptArguments),
                typeof(Plugins.InstallationPlugins.Null)
            };
        }

        private static readonly List<string> IgnoreLibraries = new() {
            "clrcompression.dll",
            "clrjit.dll",
            "coreclr.dll",
            "wacs.dll",
            "wacs.lib.dll",
            "mscordbi.dll",
            "mscordaccore.dll"
        };

        protected List<Type> LoadFromDisk()
        {
            if (string.IsNullOrEmpty(VersionService.PluginPath))
            {
                return new List<Type>();
            }
            var pluginDirectory = new DirectoryInfo(VersionService.PluginPath);
            if (!pluginDirectory.Exists)
            {
                return new List<Type>();
            }
            var dllFiles = pluginDirectory.
                EnumerateFiles("*.dll", SearchOption.AllDirectories).
                Where(x => !IgnoreLibraries.Contains(x.Name));
            if (!VersionService.Pluggable)
            {
                if (dllFiles.Any())
                {
                    _log.Error("This version of the program does not support external plugins, please download the pluggable version.");
                }
                return new List<Type>();
            } 
            else
            {
                return LoadFromDiskReal(dllFiles);
            }

        }

        protected List<Type> LoadFromDiskReal(IEnumerable<FileInfo> dllFiles)
        {
#if !PLUGGABLE
            return new List<Type>();
#else
            var allAssemblies = new List<Assembly>();
            foreach (var file in dllFiles)
            {
                try
                {
                    allAssemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(file.FullName));
                }
                catch (BadImageFormatException)
                {
                    // Not a .NET Assembly (likely runtime)
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error loading assembly {assembly}", file);
                }
            }

            var ret = new List<Type>();
            foreach (var assembly in allAssemblies)
            {
                IEnumerable<Type> types = new List<Type>();
                try
                {
                    types = GetTypesFromAssembly(assembly).ToList();
                }
                catch (ReflectionTypeLoadException rex)
                {
                    types = rex.Types.OfType<Type>();
                    foreach (var lex in rex.LoaderExceptions.OfType<Exception>())
                    {
                        _log.Error(lex, "Error loading type from {assembly}", assembly.FullName);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error loading types from assembly {assembly}", assembly.FullName);
                }
                ret.AddRange(types);
            }
            return ret;
#endif
        }

#if PLUGGABLE
        internal IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            if (assembly.DefinedTypes == null)
            {
                return new List<Type>();
            }
            return assembly.DefinedTypes.
                Where(x =>
                {
                    if (!string.IsNullOrEmpty(x.FullName) &&
                        x.FullName.StartsWith("PKISharp"))
                    {
                        return true;
                    }
                    if (x.ImplementedInterfaces != null)
                    {
                        if (x.ImplementedInterfaces.Any(x =>
                            !string.IsNullOrEmpty(x.FullName) &&
                            x.FullName.StartsWith("PKISharp")))
                        {
                            return true;
                        }

                    }
                    return false;
                }
                ).
                Select(x =>
                {
                    try
                    {
                        return x.AsType();
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error loading type {x}", x.FullName);
                        throw;
                    }
                }
                );
        }
#endif

        public List<TypeDescriptor> GetResolvable<T>()
        {
            return _allTypes.
                AsEnumerable().
                Where(type => typeof(T) != type && typeof(T).IsAssignableFrom(type) && !type.IsAbstract).
                Select(([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] type) => new TypeDescriptor() { Type = type }).
                ToList();
        }

        public struct TypeDescriptor
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
            public Type Type;
        }
    }
}