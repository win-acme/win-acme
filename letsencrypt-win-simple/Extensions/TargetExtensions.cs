using System;
using System.Linq;
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
    }
}
