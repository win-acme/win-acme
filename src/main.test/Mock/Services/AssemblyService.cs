using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class MockAssemblyService : AssemblyService
    {
        public MockAssemblyService(ILogService log) : base(log) { }

        internal override List<Type> GetTypes()
        {
            var ret = new List<Type>();
            var scanned = new List<Assembly>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                scanned.Add(assembly);
                if ((assembly.FullName ?? "").Contains("wacs") && !(assembly.FullName ?? "").Contains("test"))
                {
                    IEnumerable<Type> types = new List<Type>();
                    try
                    {
                        types = GetTypesFromAssembly(assembly).ToList();
                    }
                    catch (ReflectionTypeLoadException rex)
                    {
                        types = rex.Types?.OfType<Type>() ?? Array.Empty<Type>();
                    }
                    catch (Exception)
                    {
                    }
                    ret.AddRange(types);
                }
            }

            // Load external plugins from additional .dll files
            // put in the application folder by the user
            _log.Information(VersionService.PluginPath);
            _log.Information(ret.Count.ToString());
            ret.AddRange(LoadFromDisk(scanned));
            _log.Information(ret.Count.ToString());
            return ret;
        }

    }
}
