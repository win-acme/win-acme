using PKISharp.WACS.Configuration;
using System;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Extensions
{
    public static class MemberInfoExtensions
    {
        public static CommandLineAttribute CommandLineOptions(this MemberInfo memberInfo)
        {
            var found = Attribute.GetCustomAttributes(memberInfo).OfType<CommandLineAttribute>().FirstOrDefault();
            if (found == null)
            {
                found = new CommandLineAttribute();
            }
            if (string.IsNullOrWhiteSpace(found.MetaName))
            {
                found.MetaName = memberInfo.Name;
            }
            if (string.IsNullOrWhiteSpace(found.Description))
            {
                found.Description = $"Undocumented argument in {memberInfo.DeclaringType?.Name}";
            }
            return found;
        }
    }
}
