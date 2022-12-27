using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.Base
{
    [Obsolete]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PluginAttribute : Attribute
    {
        public Guid Id { get; set; }
        public PluginAttribute(string guid) => Id = new Guid(guid);
    }
}