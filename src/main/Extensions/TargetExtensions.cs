using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Extensions
{
    public static class TargetExtensions
    {
        public static bool IsValid(this Target target, ILogService log)
        {
            var ret = target.GetHosts(false);
            if (ret.Count > Constants.maxNames)
            {
                log.Error($"Too many hosts in a single certificate. ACME has a maximum of {Constants.maxNames} identifiers per certificate.");
                return false;
            }
            if (ret.Any(x => x.StartsWith("*")))
            {
                log.Error("Wildcard certificates are not supported yet.");
                return false;
            }
            if (ret.Count == 0)
            {
                log.Error("No valid host names provided.");
                return false;
            }
            if (ret.Contains(target.CommonName))
            {

            }
            return true;
        }

        /// <summary>
        /// Parse unique DNS identifiers that the certificate should be created for
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        public static List<string> GetHosts(this Target target, bool unicode)
        {
            return target.Parts.SelectMany(x => x.GetHosts(unicode)).Distinct().ToList();
        }


        /// <summary>
        /// Parse unique DNS identifiers that the certificate should be created for
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        public static List<string> GetHosts(this TargetPart target, bool unicode)
        {
            var idn = new IdnMapping();
            var hosts = target.Hosts.Distinct();
            if (unicode)
            {
                return hosts.Select(x => idn.GetUnicode(x)).ToList();
            }
            else
            {
                return hosts.Select(x => idn.GetAscii(x)).ToList();
            }
        }

    }
}