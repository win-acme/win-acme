using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Extensions;
using Microsoft.Win32.TaskScheduler;
using Serilog;

namespace LetsEncrypt.ACME.Simple.Schedules
{
    public class Scheduler
    {
        public static void EnsureTaskScheduler()
        {
            var taskName = $"{App.Options.ClientName} {App.Options.BaseUri.CleanFileName()}";

            using (var taskService = new TaskService())
            {
                bool addTask = true;
                if (App.Options.Settings.ScheduledTaskName == taskName)
                {
                    addTask = false;
                    if (!App.ConsoleService.PromptYesNo($"\nDo you want to replace the existing {taskName} task?"))
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

                    if (!App.Options.UseDefaultTaskUser && App.ConsoleService.PromptYesNo($"\nDo you want to specify the user the task will run as?"))
                    {
                        // Ask for the login and password to allow the task to run 
                        App.ConsoleService.WriteLine("Enter the username (Domain\\username): ");
                        var username = App.ConsoleService.ReadLine();
                        App.ConsoleService.WriteLine("Enter the user's password: ");
                        var password = App.ConsoleService.ReadPassword();
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
            Scheduler.EnsureTaskScheduler();

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
    }
}
