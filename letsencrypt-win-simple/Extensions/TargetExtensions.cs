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
        public static bool IsCommonNameValid(this Target target, ILogService failureLogService = null)
        {
            var cn = target.CommonName;
            var isValid = cn == null || target.AlternativeNames.Contains(cn, StringComparer.InvariantCultureIgnoreCase);
            if (failureLogService != null && !isValid) failureLogService.Error($"The supplied common name '{target.CommonName}' is none of this target's alternative names.");
            return isValid;
        }

        public static void AskForCommonNameChoice(this Target target, IInputService inputService)
        {
            if (target.AlternativeNames.Count < 2) return;
            var sanChoices = target.AlternativeNames.OrderBy(x => x).Select(san => Choice.Create<string>(san)).ToList();
            target.CommonName = inputService.ChooseFromList("Choose a domain name to be the certificate's common name", sanChoices, false);
        }

        /// <summary>
        /// Parse list of excluded hosts
        /// </summary>
        /// <returns></returns>
        public static List<string> GetExcludedHosts(this Target target)
        {
            var exclude = new List<string>();
            if (!string.IsNullOrEmpty(target.ExcludeBindings))
            {
                exclude = target.ExcludeBindings.Split(',').Select(x => x.ToLower().Trim()).ToList();
            }
            return exclude;
        }

        /// <summary>
        /// Parse unique DNS identifiers that the certificate should be 
        /// created for, taking into account the list of exclusions,
        /// support for IDNs and the limits of the ACME server
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        public static List<string> GetHosts(this Target target, bool unicode, bool allowZero = false)
        {
            var hosts = new List<string>();
            if (target.HostIsDns == true)
            {
                hosts.Add(target.Host);
            }
            if (target.AlternativeNames != null && target.AlternativeNames.Any())
            {
                hosts.AddRange(target.AlternativeNames);
            }
            var exclude = target.GetExcludedHosts();
            var filtered = hosts.
                Where(x => !string.IsNullOrWhiteSpace(x)).
                Distinct().
                Except(exclude);

            if (unicode)
            {
                var idn = new IdnMapping();
                filtered = filtered.Select(x => idn.GetUnicode(x));
            }
            if (!filtered.Any())
            {
                if (!allowZero)
                {
                    throw new Exception("No DNS identifiers found.");
                }
            }
            else if (filtered.Count() > Constants.maxNames)
            {
                throw new Exception($"Too many hosts for a single certificate. ACME has a maximum of {Constants.maxNames}.");
            }
            return filtered.ToList();
        }
    }
}