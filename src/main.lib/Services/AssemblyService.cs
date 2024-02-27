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
        private readonly List<TypeDescriptor> _allTypes;
        internal readonly ILogService _log;

        public AssemblyService(ILogService logger)
        {
            _log = logger;
            _allTypes = new List<TypeDescriptor>();
            _allTypes.AddRange(BuiltInTypes());
            _allTypes.AddRange(LoadFromDisk());
        }

        internal static List<TypeDescriptor> BuiltInTypes()
        {
            return new()
            {
                // Arguments
                new(typeof(Configuration.Arguments.MainArguments)),
                new(typeof(Configuration.Arguments.AccountArguments)),
                new(typeof(Configuration.Arguments.NetworkCredentialArguments)),
  
                // Target plugins
                new(typeof(Plugins.TargetPlugins.Csr)), new(typeof(Plugins.TargetPlugins.CsrArguments)),
                new(typeof(Plugins.TargetPlugins.IIS)), new(typeof(Plugins.TargetPlugins.IISArguments)),
                new(typeof(Plugins.TargetPlugins.Manual)), new(typeof(Plugins.TargetPlugins.ManualArguments)),

                // Validation plugins
                new(typeof(Plugins.ValidationPlugins.HttpValidationArguments)),
                new(typeof(Plugins.ValidationPlugins.Dns.Acme)), new(typeof(Plugins.ValidationPlugins.Dns.AcmeArguments)),
                new(typeof(Plugins.ValidationPlugins.Dns.Manual)),
                new(typeof(Plugins.ValidationPlugins.Dns.Script)), new(typeof(Plugins.ValidationPlugins.Dns.ScriptArguments)),
                new(typeof(Plugins.ValidationPlugins.Http.FileSystem)), new(typeof(Plugins.ValidationPlugins.Http.FileSystemArguments)),
                new(typeof(Plugins.ValidationPlugins.Http.Ftp)),
                new(typeof(Plugins.ValidationPlugins.Http.SelfHosting)), new(typeof(Plugins.ValidationPlugins.Http.SelfHostingArguments)),
                new(typeof(Plugins.ValidationPlugins.Http.Sftp)),
                new(typeof(Plugins.ValidationPlugins.Http.WebDav)),
                new(typeof(Plugins.ValidationPlugins.Tls.SelfHosting)), new(typeof(Plugins.ValidationPlugins.Tls.SelfHostingArguments)),

                // AcmeOrder plugins
                new(typeof(Plugins.OrderPlugins.Domain)),
                new(typeof(Plugins.OrderPlugins.Host)),
                new(typeof(Plugins.OrderPlugins.Single)),
                new(typeof(Plugins.OrderPlugins.Site)),

                // CSR plugins
                new(typeof(Plugins.CsrPlugins.CsrArguments)),
                new(typeof(Plugins.CsrPlugins.Ec)),
                new(typeof(Plugins.CsrPlugins.Rsa)),

                // Store plugins
                new(typeof(Plugins.StorePlugins.CertificateStore)), new(typeof(Plugins.StorePlugins.CertificateStoreArguments)),
                new(typeof(Plugins.StorePlugins.CentralSsl)), new(typeof(Plugins.StorePlugins.CentralSslArguments)),
                new(typeof(Plugins.StorePlugins.PemFiles)), new(typeof(Plugins.StorePlugins.PemFilesArguments)),
                new(typeof(Plugins.StorePlugins.PfxFile)), new(typeof(Plugins.StorePlugins.PfxFileArguments)),
                new(typeof(Plugins.StorePlugins.Null)),

                // Installation plugins
                new(typeof(Plugins.InstallationPlugins.IIS)), new(typeof(Plugins.InstallationPlugins.IISArguments)),
                new(typeof(Plugins.InstallationPlugins.Script)), new(typeof(Plugins.InstallationPlugins.ScriptArguments)),
                new(typeof(Plugins.InstallationPlugins.Null)),

                // Secret plugins
                new(typeof(Plugins.SecretPlugins.JsonSecretService)),

                // Notification targets
                new(typeof(Plugins.NotificationPlugins.NotificationTargetEmail))
            };
        }

        private static readonly List<string> IgnoreLibraries = new() {
            "clrcompression.dll",
            "clrjit.dll",
            "coreclr.dll",
            "wacs.dll",
            "wacs.lib.dll",
            "mscordbi.dll",
            "mscordaccore.dll",
            "Microsoft.Testing.Platform.MSBuild.dll"
        };

        protected List<TypeDescriptor> LoadFromDisk()
        {
            if (string.IsNullOrEmpty(VersionService.PluginPath))
            {
                return new List<TypeDescriptor>();
            }
            var pluginDirectory = new DirectoryInfo(VersionService.PluginPath);
            if (!pluginDirectory.Exists)
            {
                return new List<TypeDescriptor>();
            }
            var dllFiles = pluginDirectory.
                EnumerateFiles("*.dll", SearchOption.AllDirectories).
                Where(x => !IgnoreLibraries.Contains(x.Name));
            if (!VersionService.Pluggable)
            {
                if (dllFiles.Any())
                {
                    _log.Warning("This version of the program does not support external plugins, please download the pluggable version.");
                }
                return new List<TypeDescriptor>();
            } 
            else
            {
                return LoadFromDiskReal(dllFiles);
            }

        }

#if !PLUGGABLE
        protected static List<TypeDescriptor> LoadFromDiskReal(IEnumerable<FileInfo> _) => new();
#endif

#if PLUGGABLE 
        protected List<TypeDescriptor> LoadFromDiskReal(IEnumerable<FileInfo> dllFiles)
        {
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
                        _log.Error(lex, "Error loading type from {assembly} ({disk})", assembly.FullName, assembly.Location);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error loading types from assembly {assembly}", assembly.FullName);
                }
                ret.AddRange(types);
            }
            return ret.Select(t => new TypeDescriptor(t)).ToList();

        }

        internal IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            if (assembly.DefinedTypes == null)
            {
                return new List<Type>();
            }
            return assembly.DefinedTypes.
                Where(x =>
                {
                    if (x.IsAbstract)
                    {
                        return false;
                    }
                    if (!string.IsNullOrEmpty(x.FullName) && x.FullName.StartsWith("PKISharp"))
                    {
                        return true;
                    }
                    if (x.ImplementedInterfaces != null)
                    {
                        if (x.ImplementedInterfaces.Any(x => !string.IsNullOrEmpty(x.FullName) && x.FullName.StartsWith("PKISharp")))
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
        
        public virtual List<TypeDescriptor> GetResolvable<T>()
        {
            return _allTypes.
                AsEnumerable().
                Where(type => typeof(T) != type.Type && typeof(T).IsAssignableFrom(type.Type)).
                ToList();
        }

        /// <summary>
        /// Wrapper around type to convince and instruct the trimmer that the
        /// properties are preserved during the build.
        /// </summary>
        public readonly struct TypeDescriptor
        {
            public TypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type) => Type = type;
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
            public Type Type { get; init; }
        }
    }
}