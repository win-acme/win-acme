using PKISharp.WACS.Plugins.Base;
using System;
using System.Linq;

namespace PKISharp.WACS.Extensions
{
    public static class TypeExtensions
    {
        public static string? PluginId(this Type type)
        {
            var attr = type.GetCustomAttributes(true).OfType<PluginAttribute>();
            if (attr.Any())
            {
                return attr.First().Id.ToString();
            }

            return null;
        }
    }
}
