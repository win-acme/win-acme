using PKISharp.WACS.Configuration;
using System;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Extensions
{
    public static class MemberInfoExtensions
    {
        public static CommandLineAttribute? CommandLineOptions(this MemberInfo site) => Attribute.GetCustomAttributes(site).OfType<CommandLineAttribute>().FirstOrDefault();
    }
}
