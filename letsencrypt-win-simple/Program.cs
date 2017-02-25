using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using ACMESharp;
using ACMESharp.JOSE;
using Serilog;
using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Schedules;

namespace LetsEncrypt.ACME.Simple
{
    internal class Program
    {
        static bool IsElevated
            => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        private static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var app = new App();
            app.Initialize(args);
            
            Console.WriteLine("Let's Encrypt (Simple Windows ACME Client)");
            Log.Information("ACME Server: {BaseUri}", App.Options.BaseUri);
            if (App.Options.San)
                Log.Debug("San Option Enabled: Running per site and not per host");
            
            bool retry = false;
            do
            {
                try
                {
                    using (var signer = new RS256Signer())
                    {
                        signer.Init();

                        var signerPath = Path.Combine(App.Options.ConfigPath, "Signer");
                        if (File.Exists(signerPath))
                            LoadSignerFromFile(signer, signerPath);

                        using (var acmeClient = new AcmeClient(new Uri(App.Options.BaseUri), new AcmeServerDirectory(), signer))
                        {
                            var client = App.AcmeClientService.ConfigureAcmeClient(acmeClient);
                            client.Init();
                            App.Options.AcmeClient = client;

                            Log.Information("Getting AcmeServerDirectory");
                            App.Options.AcmeClient.GetDirectory(true);

                            var registrationPath = Path.Combine(App.Options.ConfigPath, "Registration");
                            if (File.Exists(registrationPath))
                                LoadRegistrationFromFile(registrationPath);
                            else
                            {
                                string email = App.Options.SignerEmail;
                                if (string.IsNullOrWhiteSpace(email))
                                {
                                    Console.Write("Enter an email address (not public, used for renewal fail notices): ");
                                    email = Console.ReadLine().Trim();
                                }

                                string[] contacts = GetContacts(email);

                                AcmeRegistration registration = App.AcmeClientService.CreateRegistration(contacts);

                                if (!App.Options.AcceptTos && !App.Options.Renew)
                                {
                                    if (!PromptYesNo($"Do you agree to {registration.TosLinkUri}?"))
                                        return;
                                }

                                UpdateRegistration(acmeClient);
                                SaveRegistrationToFile(acmeClient, registrationPath);
                                SaveSignerToFile(signer, signerPath);
                            }

                            if (App.Options.Renew)
                            {
                                CheckRenewalsAndWaitForEnterKey();
                                return;
                            }

                            List<Target> targets = GetTargetsSorted();

                            WriteBindings(targets);

                            Console.WriteLine();
                            PrintMenuForPlugins();

                            if (string.IsNullOrEmpty(App.Options.ManualHost) && string.IsNullOrWhiteSpace(App.Options.Plugin))
                            {
                                Console.WriteLine(" A: Get certificates for all hosts");
                                Console.WriteLine(" Q: Quit");
                                Console.Write("Which host do you want to get a certificate for: ");
                                var command = ReadCommandFromConsole();
                                switch (command)
                                {
                                    case "a":
                                        App.CertificateService.GetCertificatesForAllHosts(targets);
                                        break;
                                    case "q":
                                        return;
                                    default:
                                        ProcessDefaultCommand(targets, command);
                                        break;
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(App.Options.Plugin))
                            {
                                // If there's a plugin in the options, only do ProcessDefaultCommand for the selected plugin
                                // Plugins that can run automatically should allow for an empty string as menu response to work
                                ProcessDefaultCommand(targets, string.Empty);
                            }
                        }
                    }

                    retry = false;
                    if (string.IsNullOrWhiteSpace(App.Options.Plugin))
					{
	                    Console.WriteLine("Press enter to continue.");
	                    Console.ReadLine();
					}
                }
                catch (Exception e)
                {
                    Environment.ExitCode = e.HResult;

                    Log.Error("Error {@e}", e);
                    var acmeWebException = e as AcmeClient.AcmeWebException;
                    if (acmeWebException != null)
						Log.Error("ACME Server Returned: {acmeWebExceptionMessage} - Response: {acmeWebExceptionResponse}", acmeWebException.Message, acmeWebException.Response.ContentAsString);
                }

                if (string.IsNullOrWhiteSpace(App.Options.Plugin) && App.Options.Renew)
                {
                    Console.WriteLine("Would you like to start again? (y/n)");
                    if (ReadCommandFromConsole() == "y")
                        retry = true;
                }
            } while (retry);
        }

        private static void ProcessDefaultCommand(List<Target> targets, string command)
        {
            var targetId = 0;
            if (Int32.TryParse(command, out targetId))
            {
                App.CertificateService.GetCertificateForTargetId(targets, targetId);
                return;
            }

            HandleMenuResponseForPlugins(targets, command);
        }

        private static void HandleMenuResponseForPlugins(List<Target> targets, string command)
        {
            // Only run the plugin specified in the config
            if (!string.IsNullOrWhiteSpace(App.Options.Plugin))
            {
                var plugin = Target.Plugins.Values.FirstOrDefault(x => string.Equals(x.Name, App.Options.Plugin, StringComparison.InvariantCultureIgnoreCase));
                if (plugin != null)
                    plugin.HandleMenuResponse(command, targets);
                else
                {
                    Console.WriteLine($"Plugin '{App.Options.Plugin}' could not be found. Press enter to exit.");
                    Console.ReadLine();
                }
            }
            else
            {
                foreach (var plugin in Target.Plugins.Values)
                    plugin.HandleMenuResponse(command, targets);
            }
        }
        
        private static void CheckRenewalsAndWaitForEnterKey()
        {
            CheckRenewals();
            WaitForEnterKey();
        }

        private static void WaitForEnterKey()
        {
#if DEBUG
            Console.WriteLine("Press enter to continue.");
            Console.ReadLine();
#endif
        }

        private static void LoadRegistrationFromFile(string registrationPath)
        {
            Log.Information("Loading Registration from {registrationPath}", registrationPath);
            using (var registrationStream = File.OpenRead(registrationPath))
                App.Options.AcmeClient.Registration = AcmeRegistration.Load(registrationStream);
        }

        private static string[] GetContacts(string email)
        {
            var contacts = new string[] { };
            if (!String.IsNullOrEmpty(email))
            {
                Log.Debug("Registration email: {email}", email);
                email = "mailto:" + email;
                contacts = new string[] { email };
            }

            return contacts;
        }

        private static void SaveSignerToFile(RS256Signer signer, string signerPath)
        {
            Log.Information("Saving Signer");
            using (var signerStream = File.OpenWrite(signerPath))
                signer.Save(signerStream);
        }

        private static void SaveRegistrationToFile(AcmeClient acmeClient, string registrationPath)
        {
            Log.Information("Saving Registration");
            using (var registrationStream = File.OpenWrite(registrationPath))
                acmeClient.Registration.Save(registrationStream);
        }

        private static void UpdateRegistration(AcmeClient acmeClient)
        {
            Log.Information("Updating Registration");
            acmeClient.UpdateRegistration(true, true);
        }

        private static void WriteBindings(List<Target> targets)
        {
            if (targets.Count == 0 && string.IsNullOrEmpty(App.Options.ManualHost))
                WriteNoTargetsFound();
            else
            {
                int hostsPerPage = GetHostsPerPageFromSettings();

                if (targets.Count > hostsPerPage)
                    WriteBindingsFromTargetsPaged(targets, hostsPerPage, 1);
                else
                    WriteBindingsFromTargetsPaged(targets, targets.Count, 1);
            }
        }

        private static void PrintMenuForPlugins()
        {
            // Check for a plugin specified in the options
            // Only print the menus if there's no plugin specified
            // Otherwise: you actually have no choice, the specified plugin will run
            if (!string.IsNullOrWhiteSpace(App.Options.Plugin))
                return;

            foreach (var plugin in Target.Plugins.Values)
            {
                if (string.IsNullOrEmpty(App.Options.ManualHost))
                {
                    plugin.PrintMenu();
                }
                else if (plugin.Name == "Manual")
                {
                    plugin.PrintMenu();
                }
            }
        }

        private static int GetHostsPerPageFromSettings()
        {
            int hostsPerPage = 50;
            try
            {
                hostsPerPage = Properties.Settings.Default.HostsPerPage;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting HostsPerPage setting, setting to default value. Error: {@ex}",
                    ex);
            }

            return hostsPerPage;
        }

        private static void WriteNoTargetsFound()
        {
            Log.Error("No targets found.");
        }

        private static void LoadSignerFromFile(RS256Signer signer, string signerPath)
        {
            Log.Information("Loading Signer from {signerPath}", signerPath);
            using (var signerStream = File.OpenRead(signerPath))
                signer.Load(signerStream);
        }
        
        private static int WriteBindingsFromTargetsPaged(List<Target> targets, int pageSize, int fromNumber)
        {
            do
            {
                int toNumber = fromNumber + pageSize;
                if (toNumber <= targets.Count)
                    fromNumber = WriteBindingsFomTargets(targets, toNumber, fromNumber);
                else
                    fromNumber = WriteBindingsFomTargets(targets, targets.Count + 1, fromNumber);

                if (fromNumber < targets.Count)
                {
                    WriteQuitCommandInformation();
                    string command = ReadCommandFromConsole();
                    switch (command)
                    {
                        case "q":
                            throw new Exception($"Requested to quit application");
                        default:
                            break;
                    }
                }
            } while (fromNumber < targets.Count);

            return fromNumber;
        }

        private static string ReadCommandFromConsole()
        {
            return Console.ReadLine().ToLowerInvariant();
        }

        private static void WriteQuitCommandInformation()
        {
            Console.WriteLine(" Q: Quit");
            Console.Write("Press enter to continue to next page ");
        }

        private static int WriteBindingsFomTargets(List<Target> targets, int toNumber, int fromNumber)
        {
            for (int i = fromNumber; i < toNumber; i++)
            {
                if (!App.Options.San)
                {
                    Console.WriteLine($" {i}: {targets[i - 1]}");
                }
                else
                {
                    Console.WriteLine($" {targets[i - 1].SiteId}: SAN - {targets[i - 1]}");
                }
                fromNumber++;
            }

            return fromNumber;
        }

        private static List<Target> GetTargetsSorted()
        {
            var targets = new List<Target>();
            if (!string.IsNullOrEmpty(App.Options.ManualHost))
                return targets;

            foreach (var plugin in Target.Plugins.Values)
            {
                targets.AddRange(!App.Options.San ? plugin.GetTargets() : plugin.GetSites());
            }

            return targets.OrderBy(p => p.ToString()).ToList();
        }

        public static bool PromptYesNo(string message)
        {
            while (true)
            {
                var response = Console.ReadKey(true);
                if (response.Key == ConsoleKey.Y)
                    return true;
                if (response.Key == ConsoleKey.N)
                    return false;
                Console.WriteLine(message + " (y/n)");
            }
        }

        public static void Auto(Target binding)
        {
            var auth = Authorize(binding);
            if (auth.Status == "valid")
            {
                var pfxFilename = GetCertificate(binding);

                if (App.Options.Test && !App.Options.Renew)
                {
                    if (!PromptYesNo($"\nDo you want to install the .pfx into the Certificate Store/ Central SSL Store?"))
                        return;
                }

                if (!App.Options.CentralSsl)
                {
                    X509Store store;
                    X509Certificate2 certificate;
                    Log.Information("Installing Non-Central SSL Certificate in the certificate store");
                    InstallCertificate(binding, pfxFilename, out store, out certificate);
                    if (App.Options.Test && !App.Options.Renew)
                    {
                        if (!PromptYesNo($"\nDo you want to add/update the certificate to your server software?"))
                            return;
                    }
                    Log.Information("Installing Non-Central SSL Certificate in server software");
                    binding.Plugin.Install(binding, pfxFilename, store, certificate);
                    if (!App.Options.KeepExisting)
                    {
                        UninstallCertificate(binding.Host, out store, certificate);
                    }
                }
                else if (!App.Options.Renew || !App.Options.KeepExisting)
                {
                    //If it is using centralized SSL, renewing, and replacing existing it needs to replace the existing binding.
                    Log.Information("Updating new Central SSL Certificate");
                    binding.Plugin.Install(binding);
                }

                if (App.Options.Test && !App.Options.Renew)
                {
                    if (!PromptYesNo($"\nDo you want to automatically renew this certificate in {App.Options.RenewalPeriodDays} days? This will add a task scheduler task."))
                        return;
                }

                if (!App.Options.Renew)
                {
                    Log.Information("Adding renewal for {binding}", binding);
                    ScheduleRenewal(binding);
                }
            }
        }
        public static void EnsureTaskScheduler()
        {
            var taskName = $"{App.Options.ClientName} {App.Options.BaseUri.CleanFileName()}";

            using (var taskService = new TaskService())
            {
                bool addTask = true;
                if (App.Options.Settings.ScheduledTaskName == taskName)
                {
                    addTask = false;
                    if (!PromptYesNo($"\nDo you want to replace the existing {taskName} task?"))
                        return;
                    addTask = true;
                    Log.Information("Deleting existing Task {taskName} from Windows Task Scheduler.", taskName);
                    taskService.RootFolder.DeleteTask(taskName, false);
                }

                if (addTask == true)
                {
                    Log.Information("Creating Task {taskName} with Windows Task scheduler at 9am every day.", taskName);

                    // Create a new task definition and assign properties
                    var task = taskService.NewTask();
                    task.RegistrationInfo.Description = "Check for renewal of ACME certificates.";

                    var now = DateTime.Now;
                    var runtime = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0);
                    task.Triggers.Add(new DailyTrigger { DaysInterval = 1, StartBoundary = runtime });

                    var currentExec = Assembly.GetExecutingAssembly().Location;

                    // Create an action that will launch the app with the renew parameters whenever the trigger fires
                    string actionString = $"--renew --baseuri \"{App.Options.BaseUri}\"";
                    if (!string.IsNullOrWhiteSpace(App.Options.CertOutPath))
                        actionString += $" --certoutpath \"{App.Options.CertOutPath}\"";
                    task.Actions.Add(new ExecAction(currentExec, actionString,
                        Path.GetDirectoryName(currentExec)));

                    task.Principal.RunLevel = TaskRunLevel.Highest; // need admin
                    Log.Debug("{@task}", task);

                    if (!App.Options.UseDefaultTaskUser && PromptYesNo($"\nDo you want to specify the user the task will run as?"))
                    {
                        // Ask for the login and password to allow the task to run 
                        Console.Write("Enter the username (Domain\\username): ");
                        var username = Console.ReadLine();
                        Console.Write("Enter the user's password: ");
                        var password = ReadPassword();
                        Log.Debug("Creating task to run as {username}", username);
                        taskService.RootFolder.RegisterTaskDefinition(taskName, task, TaskCreation.Create, username,
                            password, TaskLogonType.Password);
                    }
                    else
                    {
                        Log.Debug("Creating task to run as current user only when the user is logged on");
                        taskService.RootFolder.RegisterTaskDefinition(taskName, task);
                    }
                    App.Options.Settings.ScheduledTaskName = taskName;
                }
            }
        }



        public static void ScheduleRenewal(Target target)
        {
            EnsureTaskScheduler();

            var renewals = App.Options.Settings.LoadRenewals();

            foreach (var existing in from r in renewals.ToArray() where r.Binding.Host == target.Host select r)
            {
                Log.Information("Removing existing scheduled renewal {existing}", existing);
                renewals.Remove(existing);
            }

            var result = new ScheduledRenewal()
            {
                Binding = target,
                CentralSsl = App.Options.CentralSslStore,
                San = App.Options.San.ToString(),
                Date = DateTime.UtcNow.AddDays(App.Options.RenewalPeriodDays),
                KeepExisting = App.Options.KeepExisting.ToString(),
                Script = App.Options.Script,
                ScriptParameters = App.Options.ScriptParameters,
                Warmup = App.Options.Warmup
            };
            renewals.Add(result);
            App.Options.Settings.SaveRenewals(renewals);

            Log.Information("Renewal Scheduled {result}", result);
        }

        public static void CheckRenewals()
        {
            Log.Information("Checking Renewals");

            var renewals = App.Options.Settings.LoadRenewals();
            if (renewals.Count == 0)
                Log.Information("No scheduled renewals found.");

            var now = DateTime.UtcNow;
            foreach (var renewal in renewals)
                ProcessRenewal(renewals, now, renewal);
        }

        private static void ProcessRenewal(List<ScheduledRenewal> renewals, DateTime now, ScheduledRenewal renewal)
        {
            Log.Information("Checking {renewal}", renewal);
            if (renewal.Date >= now) return;

            Log.Information("Renewing certificate for {renewal}", renewal);
            if (string.IsNullOrWhiteSpace(renewal.CentralSsl))
            {
                //Not using Central SSL
                App.Options.CentralSsl = false;
                App.Options.CentralSslStore = null;
            }
            else
            {
                //Using Central SSL
                App.Options.CentralSsl = true;
                App.Options.CentralSslStore = renewal.CentralSsl;
            }
            if (string.IsNullOrWhiteSpace(renewal.San))
            {
                //Not using San
                App.Options.San = false;
            }
            else if (renewal.San.ToLower() == "true")
            {
                //Using San
                App.Options.San = true;
            }
            else
            {
                //Not using San
                App.Options.San = false;
            }
            if (string.IsNullOrWhiteSpace(renewal.KeepExisting))
            {
                //Not using KeepExisting
                App.Options.KeepExisting = false;
            }
            else if (renewal.KeepExisting.ToLower() == "true")
            {
                //Using KeepExisting
                App.Options.KeepExisting = true;
            }
            else
            {
                //Not using KeepExisting
                App.Options.KeepExisting = false;
            }
            if (!string.IsNullOrWhiteSpace(renewal.Script))
            {
                App.Options.Script = renewal.Script;
            }
            if (!string.IsNullOrWhiteSpace(renewal.ScriptParameters))
            {
                App.Options.ScriptParameters = renewal.ScriptParameters;
            }
            if (renewal.Warmup)
            {
                App.Options.Warmup = true;
            }
            renewal.Binding.Plugin.Renew(renewal.Binding);

            renewal.Date = DateTime.UtcNow.AddDays(App.Options.RenewalPeriodDays);
            App.Options.Settings.SaveRenewals(renewals);

            Log.Information("Renewal Scheduled {renewal}", renewal);
        }
    }
}
