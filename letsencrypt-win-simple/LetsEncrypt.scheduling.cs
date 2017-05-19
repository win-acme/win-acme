using CommandLine;
using Microsoft.Win32.TaskScheduler;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple
{
    partial class LetsEncrypt
    {
        internal static void ScheduleRenewal(Target target)
        {
            Settings settings = new Settings(Options.ConfigPath);

            EnsureTaskScheduler(settings);

            Log.Information("Adding renewal for {target}", target);
            var renewals = settings.Renewals;

            foreach (var existing in from r in renewals.ToArray() where r.Binding.Host == target.Host select r)
            {
                Log.Information("Removing existing scheduled renewal {existing}", existing);
                renewals.Remove(existing);
            }

            var result = new ScheduledRenewal
            {
                Binding = target,
                CentralSsl = Options.CentralSslStore,
                San = Options.San.ToString(),
                Date = DateTime.UtcNow.AddDays(Options.RenewalPeriod),
                KeepExisting = Options.KeepExisting.ToString(),
                Script = Options.Script,
                ScriptParameters = Options.ScriptParameters,
                Warmup = Options.Warmup
            };
            renewals.Add(result);
            settings.Save();

            Log.Information("Renewal Scheduled {result}", result);
        }

        private static string CleanFileName(string fileName)
        {
            return
                Path.GetInvalidFileNameChars()
                    .Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        internal static void EnsureTaskScheduler(Settings settings)
        {
            var taskName = $"{CLIENT_NAME} {CleanFileName(Options.BaseUri)}";

            using (var taskService = new TaskService())
            {
                bool addTask = true;
                if (settings.ScheduledTaskName == taskName)
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
                    string actionString = $"--renew --baseuri \"{Options.BaseUri}\"";
                    if (!string.IsNullOrWhiteSpace(Options.CertOutPath))
                        actionString += $" --certoutpath \"{Options.CertOutPath}\"";
                    task.Actions.Add(new ExecAction(currentExec, actionString,
                        Path.GetDirectoryName(currentExec)));

                    task.Principal.RunLevel = TaskRunLevel.Highest; // need admin
                    Log.Debug("{@task}", task);

                    if (!Options.Silent && !Options.UseDefaultTaskUser && PromptYesNo($"\nDo you want to specify the user the task will run as?"))
                    {
                        // Ask for the login and password to allow the task to run 
                        Console.Write("Enter the username (Domain\\username): ");
                        var username = Console.ReadLine();
                        Console.Write("Enter the user's password: ");
                        var password = ReadPassword();
                        Log.Debug("Creating task to run as {username}", username);
                        taskService.RootFolder.RegisterTaskDefinition(taskName, task, TaskCreation.Create, username,
                            password.ToString(), TaskLogonType.Password);
                    }
                    else
                    {
                        Log.Debug("Creating task to run as current user only when the user is logged on");
                        taskService.RootFolder.RegisterTaskDefinition(taskName, task);
                    }
                    settings.ScheduledTaskName = taskName;
                }
            }
        }
    }
}
