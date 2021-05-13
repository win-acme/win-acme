using System;

namespace PKISharp.WACS.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CommandLineAttribute : Attribute
    {
        public string Name {
            get => MetaName ?? throw new InvalidOperationException();
            set => MetaName = value;
        }
        internal string? MetaName { get; set; }
        public string ArgumentName => Name.ToLower();
        public string? Description { get; set; }
        public string? Default { get; set; }
        public bool Obsolete { get; set; }
        public bool Secret { get; set; }
    }
}
