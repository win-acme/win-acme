using System;

namespace PKISharp.WACS.Plugins.Base
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PluginAttribute : Attribute
    {
        public Guid Id { get; set; }
        public PluginAttribute(string guid)
        {
            Id = new Guid(guid);
        }
    }
}