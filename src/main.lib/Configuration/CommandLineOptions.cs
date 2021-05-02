using System;

namespace PKISharp.WACS.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CommandLineAttribute : Attribute
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Default { get; set; }
        public bool Obsolete { get; set; } = false;
        public CommandLineAttribute(string? Name = null, string? Description = null, bool Obsolete = false)
        {
            this.Name = Name;
            this.Obsolete = Obsolete;
            this.Description = Description;
        }
    }
}
