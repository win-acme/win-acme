using Autofac;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PKISharp.WACS.DomainObjects
{
    public class Target
    {
        /// <summary>
        /// Friendly name of the certificate, which may or may
        /// no also be the common name (first host), as indicated
        /// by the <see cref="HostIsDns"/> property.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Is the name of the certificate also a DNS identifier?
        /// </summary>
        public bool HostIsDns { get; set; }

        /// <summary>
        /// The common name of the certificate. Has to be one of
        /// the alternative names.
        /// </summary>
        public string CommonName { get; set; }

        /// <summary>
        /// Hide target from the user in interactive mode, i.e.
        /// because some filter has been applied (--hidehttps).
        /// </summary>
        [JsonIgnore] public bool? Hidden { get; set; }

        /// <summary>
        /// Triggers IIS specific behaviours, such as copying
        /// the web.config file in case of Http validation
        /// </summary>
        [JsonIgnore] public bool IIS { get => TargetSiteId != null; }

        /// <summary>
        /// Site used to get bindings from
        /// </summary>
        public long? TargetSiteId { get; set; }

        /// <summary>
        /// List of bindings to exclude from the certificate
        /// </summary>
        public string ExcludeBindings { get; set; }

        /// <summary>
        /// List of alternative names for the certificate
        /// </summary>
        public List<string> AlternativeNames { get; set; } = new List<string>();

        /// <summary>
        /// Name of the plugin to use for refreshing the target
        /// </summary>
        public string TargetPluginName { get; set; }

        /// <summary>
        /// Pretty print information about the target
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var x = new StringBuilder();
            x.Append($"[{TargetPluginName}] ");
            if (!AlternativeNames.Contains(Host))
            {
                x.Append($"{Host} ");
            }
            if ((TargetSiteId ?? 0) > 0)
            {
                x.Append($"(SiteId {TargetSiteId.Value}) ");
            }
            x.Append("[");
            var num = AlternativeNames.Count();
            if (num > 0)
            {
                x.Append($"{num} binding");
                if (num > 1)
                {
                    x.Append($"s");
                }
                x.Append($" - {AlternativeNames.First()}");
                if (num > 1)
                {
                    x.Append($", ...");
                }
            }
            x.Append("]");
            return x.ToString();
        }
    }
}