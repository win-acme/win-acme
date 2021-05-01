using PKISharp.WACS.Services;

namespace PKISharp.WACS.Configuration.Arguments
{
    public abstract class BaseArguments : IArgumentsStandalone
    {
        public abstract string Name { get; }
        public virtual string Group => "";
        public virtual string Condition => "";
        public virtual bool Default => false;
        public virtual bool Active() => false;
        public bool Validate(MainArguments main) => true;
    }
}