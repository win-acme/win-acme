using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;
using System.Linq;
using static LetsEncrypt.ACME.Simple.Services.InputService;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISSite : IISClient, ITargetPlugin
    {
        string IHasName.Name => nameof(IISSite);
        string IHasName.Description => "All bindings for a single IIS site";
      
        Target ITargetPlugin.Default(Options options)
        {
            if (!string.IsNullOrEmpty(options.SiteId))
            {
                long siteId = 0;
                if (long.TryParse(options.SiteId, out siteId))
                {
                    var found = GetSites(options, false).FirstOrDefault(binding => binding.SiteId == siteId);
                    if (found != null)
                    {
                        found.ExcludeBindings = options.ExcludeBindings;
                        return found;
                    }
                    else
                    {
                        Program.Log.Error("Unable to find site with id {siteId}", siteId);
                    }
                }
                else
                {
                    Program.Log.Error("Invalid SiteId {siteId}", options.SiteId);
                }
            }
            else
            {
                Program.Log.Error("Please specify the --siteid argument");
            }
            return null;
        }

        Target ITargetPlugin.Aquire(Options options, InputService input)
        {
            var chosen = input.ChooseFromList("Choose site",
                GetSites(options, true).Where(x => x.Hidden == false),
                x => new Choice<Target>(x) { description = x.Host },
                true);
            if (chosen != null)
            {
                // Exclude bindings 
                input.WritePagedList(chosen.AlternativeNames.Select(x => Choice.Create(x, "")));
                chosen.ExcludeBindings = input.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions");
                return chosen;
            }
            return null;
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            var match = GetSites(options, false).FirstOrDefault(binding => binding.Host == scheduled.Host);
            if (match != null)
            {
                UpdateWebRoot(scheduled, match);
                UpdateAlternativeNames(scheduled, match);
                return scheduled;
            }
            return null;
        }
    }
}