using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Get the plugin identifier
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string? PluginId(this Type type)
        {
            var attr = type.GetCustomAttributes(true).OfType<PluginAttribute>();
            if (attr.Any())
            {
                return attr.First().Id.ToString();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get all metadata about this class
        /// </summary>
        public static IEnumerable<(CommandLineAttribute, PropertyInfo)> CommandLineProperties(this Type type)
        {
            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            foreach (var property in allProperties)
            {
                var declaringType = property.GetSetMethod()?.GetBaseDefinition().DeclaringType;
                if (declaringType == null)
                {
                    continue;
                }
                var isLocal = declaringType == type || declaringType.IsAbstract;
                if (!isLocal)
                {
                    continue;
                }
                var commandLineInfo = property.CommandLineOptions();
                yield return (commandLineInfo, property);
            }
        }
    }
}
