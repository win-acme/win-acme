using PKISharp.WACS.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static PKISharp.WACS.Services.AssemblyService;

namespace PKISharp.WACS.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Get all metadata about this class
        /// </summary>
        public static IEnumerable<(CommandLineAttribute, PropertyInfo, TypeDescriptor)> CommandLineProperties
            ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type)
        {
            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
                if (property.PropertyType == typeof(string))
                {
                    yield return (commandLineInfo, property, new TypeDescriptor(typeof(string)));
                }
                else if (property.PropertyType == typeof(bool))
                {
                    yield return (commandLineInfo, property, new TypeDescriptor(typeof(bool)));
                }
                else if (property.PropertyType == typeof(int?))
                {
                    yield return (commandLineInfo, property, new TypeDescriptor(typeof(int?)));
                }
                else if (property.PropertyType == typeof(long?))
                {
                    yield return (commandLineInfo, property, new TypeDescriptor(typeof(long?)));
                }
                else
                {
                    throw new NotSupportedException($"Argument {type.Name}.{property.Name} has unsupported type {property.PropertyType.FullName}");
                }
            }
        }
    }
}
