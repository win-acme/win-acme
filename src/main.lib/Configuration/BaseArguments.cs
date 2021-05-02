using PKISharp.WACS.Services;
using System.Reflection;

namespace PKISharp.WACS.Configuration.Arguments
{
    public abstract class BaseArguments : IArgumentsStandalone
    {
        public abstract string Name { get; }
        public virtual string Group => "";
        public virtual string Condition => "";
        public virtual bool Default => false;
        public virtual bool Active() 
        {
            foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var isLocal = prop.GetGetMethod()?.GetBaseDefinition().DeclaringType == GetType();
                if (!isLocal)
                {
                    continue;
                }
                if (prop.PropertyType == typeof(bool) && (bool)(prop.GetValue(this) ?? false) == true)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(string) && !string.IsNullOrEmpty((string)(prop.GetValue(this) ?? string.Empty)))
                {
                    return true;
                }
                if (prop.PropertyType == typeof(int) && (int)(prop.GetValue(this) ?? 0) > 0)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(int?) && (int?)prop.GetValue(this) != null)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(long) && (long)(prop.GetValue(this) ?? 0) > 0)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(long?) && (long?)prop.GetValue(this) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Validate(MainArguments main) => true;
    }
}