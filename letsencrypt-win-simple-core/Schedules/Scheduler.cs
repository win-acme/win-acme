using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LetsEncrypt.ACME.Simple.Core.Configuration;
using LetsEncrypt.ACME.Simple.Core.Extensions;
using LetsEncrypt.ACME.Simple.Core.Interfaces;
using Microsoft.Win32.TaskScheduler;
using Serilog;

namespace LetsEncrypt.ACME.Simple.Core.Schedules
{
    public class Scheduler
    {
        protected IOptions Options;
        protected IConsoleService ConsoleService;
        public Scheduler(IOptions options, IConsoleService consoleService)
        {
            Options = options;
            ConsoleService = consoleService;
        }

        public void EnsureTaskScheduler()
        {
            var taskName = $"{Options.ClientName} {Options.BaseUri.CleanFileName()}";

            using (var taskService = new TaskService())
            {
                if (Options.Settings.ScheduledTaskName == taskName)
                {

                    if (!ConsoleService.PromptYesNo($"\nDo you want to replace the existing {taskName} task?"))
                        return;

                    Log.Information("Deleting existing Task {taskName} from Windows Task Scheduler.", taskName);
                    taskService.RootFolder.DeleteTask(taskName, false);
                }

                Log.Information("Creating Task {taskName} with Windows Task scheduler at 9am every day.", taskName);

                // Create a new task definition and assign properties
                var task = taskService.NewTask();
                task.RegistrationInfo.Description = "Check for renewal of ACME certificates.";

                var now = DateTime.Now;
                var runtime = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0);
                task.Triggers.Add(new DailyTrigger { DaysInterval = 1, StartBoundary = runtime });

                var currentExec = Assembly.GetExecutingAssembly().Location;

                // Create an action that will launch the app with the renew parameters whenever the trigger fires
                string actionString = $"--renew --baseuri \"{Options.BaseUri}\"";
                if (!string.IsNullOrWhiteSpace(Options.CertOutPath))
                    actionString += $" --certoutpath \"{Options.CertOutPath}\"";
                task.Actions.Add(new ExecAction(currentExec, actionString,
                    Path.GetDirectoryName(currentExec)));

                task.Principal.RunLevel = TaskRunLevel.Highest; // need admin
                Log.Debug("{@task}", task);

                if (!Options.UseDefaultTaskUser && ConsoleService.PromptYesNo($"\nDo you want to specify the user the task will run as?"))
                {
                    // Ask for the login and password to allow the task to run 
                    ConsoleService.WriteLine("Enter the username (Domain\\username): ");
                    var username = ConsoleService.ReadLine();
                    ConsoleService.WriteLine("Enter the user's password: ");
                    var password = ConsoleService.ReadPassword();
                    Log.Debug("Creating task to run as {username}", username);
                    taskService.RootFolder.RegisterTaskDefinition(taskName, task, TaskCreation.Create, username,
                        password, TaskLogonType.Password);
                }
                else
                {
                    Log.Debug("Creating task to run as current user only when the user is logged on");
                    taskService.RootFolder.RegisterTaskDefinition(taskName, task);
                }

                Options.Settings.ScheduledTaskName = taskName;
            }
        }

        public void ScheduleRenewal(Target target)
        {
            EnsureTaskScheduler();

            var renewals = Options.Settings.LoadRenewals();

            foreach (var existing in from r in renewals.ToArray() where r.Binding.Host == target.Host select r)
            {
                Log.Information("Removing existing scheduled renewal {existing}", existing);
                renewals.Remove(existing);
            }

            var result = new ScheduledRenewal()
            {
                Binding = target,
                CentralSsl = Options.CentralSslStore,
                San = Options.San.ToString(),
                Date = DateTime.UtcNow.AddDays(Options.RenewalPeriodDays),
                KeepExisting = Options.KeepExisting.ToString(),
                Script = Options.Script,
                ScriptParameters = Options.ScriptParameters,
                Warmup = Options.Warmup
            };
            renewals.Add(result);
            Options.Settings.SaveRenewals(renewals);

            Log.Information("Renewal Scheduled {result}", result);
        }
    }
}
