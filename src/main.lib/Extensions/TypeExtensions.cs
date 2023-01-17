using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Get all metadata about this class
        /// </summary>
        public static IEnumerable<(CommandLineAttribute, PropertyInfo)> CommandLineProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type)
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
