using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Configuration.Arguments
{
    public abstract class BaseArguments : IArguments
    {
        public abstract string Name { get; }
        public virtual string Group => "";
        public virtual string Condition => "";
        public virtual bool Default => false;
        public virtual bool Active(string[] args) 
        {
            foreach (var (meta, _) in GetType().CommandLineProperties())
            {
                if (args.Select(x => x.ToLower()).Contains(meta.ArgumentName.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}