using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PKISharp.WACS.Extensions
{
    public static class TargetExtensions
    {
        public static bool IsValid(this Target target, ILogService log)
        {
            var ret = target.GetIdentifiers(true);
            if (ret.Count > Constants.MaxNames)
            {
                log.Error($"Too many identifiers in a single certificate. ACME has a maximum of {Constants.MaxNames} identifiers per certificate.");
                return false;
            }
            if (ret.Count == 0)
            {
                log.Error("No valid identifiers provided.");
                return false;
            }
            if (!ret.Contains(target.CommonName))
            {
                log.Error("Common name not contained in SAN list.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Parse unique DNS identifiers that the certificate should be created for
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        public static List<Identifier> GetIdentifiers(this Target target, bool unicode) => 
            target.Parts.SelectMany(x => x.GetIdentifiers(unicode)).Distinct().ToList();

        /// <summary>
        /// Parse unique DNS identifiers that the certificate should be created for
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        public static List<Identifier> GetIdentifiers(this TargetPart part, bool unicode) => 
            part.Identifiers.Distinct().Select(x => x.Unicode(unicode)).ToList();

    }
}