using LetsEncrypt.ACME.Simple.Services;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISSite : IISPlugin, ITargetPlugin
    {
        string IHasName.Name
        {
            get
            {
                return nameof(IISSite);
            }
        }

        string ITargetPlugin.Description
        {
            get
            {
                return "All bindings for a single IIS site";
            }
        }

        Target ITargetPlugin.Default(Options options)
        {
            if (!string.IsNullOrEmpty(options.SiteId))
            {
                long siteId = 0;
                if (long.TryParse(options.SiteId, out siteId))
                {
                    var found = GetSites().FirstOrDefault(binding => binding.SiteId == siteId);
                    if (found != null)
                    {
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

        Target ITargetPlugin.Aquire(Options options)
        {
            return Program.Input.ChooseFromList("Choose site",
                GetSites(),
                x => new InputService.Choice<Target>(x) { description = x.Host },
                true);
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            var match = GetSites().FirstOrDefault(binding => binding.Host == scheduled.Host);
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