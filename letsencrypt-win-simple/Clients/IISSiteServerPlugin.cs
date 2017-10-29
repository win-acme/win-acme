using ACMESharp;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple.Clients
{
    public class IISSiteServerPlugin : IISClient
    {
        public new const string PluginName = "IISSiteServer";

        public override string Name => PluginName;

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 newCertificate, X509Certificate2 oldCertificate)
        {
            foreach (var subTarget in SplitTarget(target))
            {
                subTarget.Plugin.Install(subTarget, pfxFilename, store, newCertificate, oldCertificate);
            }
        }

        public override void Install(Target target)
        {
            foreach (var subTarget in SplitTarget(target))
            {
                subTarget.Plugin.Install(subTarget);
            }
        }

        public override RenewResult Auto(Target target)
        {
            foreach (var subTarget in SplitTarget(target))
            {
                var auth = Program.Authorize(subTarget);
                if (auth.Status != "valid")
                {
                    return Program.OnAutoFail(auth);
                }
            }
            return Program.OnAutoSuccess(target);
        }

        private List<Target> SplitTarget(Target totalTarget)
        {
            var plugin = (IISSite)Program.Plugins.GetByName(Program.Plugins.Target, nameof(IISSite));
            List<Target> targets = plugin.GetSites(_optionsService.Options, false);
            string[] siteIDs = totalTarget.Host.Split(',');
            var filtered = targets.Where(t => siteIDs.Contains(t.SiteId.ToString())).ToList();
            filtered.ForEach(x => {
                x.ExcludeBindings = totalTarget.ExcludeBindings;
                x.ValidationPluginName = totalTarget.ValidationPluginName;
            });

            return filtered;
        }
    }
}