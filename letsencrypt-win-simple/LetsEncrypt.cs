using Serilog;
using System;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple
{
    partial class LetsEncrypt
    {
        private static void Main(string[] args)
        {
            CreateLogger();
            try
            {
                ParseOptions(args);
                SelectPlugin();
                if (SelectedPlugin != null && Validate())
                {
                    using (var client = CreateAcmeClient())
                    {
                        SelectedPlugin.client = client;
                        if (SelectedPlugin.SelectOptions(Options))
                        {
                            SelectedPlugin.AlternativeNames = GetAlternativeNames();
                            List<Target> targets = SelectedPlugin.GetTargets();
                            foreach (Target target in targets)
                            {
                                InstallTarget(target);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(R.Anunhandledexceptionoccurred, e);
                Environment.ExitCode = 1;
            }
        }

        private static void InstallTarget(Target target)
        {
            if (Options.Renew)
            {
                SelectedPlugin.Renew(target, Options);
            }
            else
            {
                SelectedPlugin.Install(target, Options);
                string message = string.Format(R.Doyouwanttoautomaticallyrenewthiscertificate, Options.RenewalPeriod);
                if (PromptYesNo(message))
                {
                    ScheduleRenewal(target);
                }
            }
        }
    }
}
