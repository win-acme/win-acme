using System;

namespace PKISharp.WACS.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CommandLineAttribute : Attribute
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Default { get; set; }
        public CommandLineAttribute(string? Name = null, string? Description = null)
        {
            this.Name = Name;
            this.Description = Description;
        }
    }
}
