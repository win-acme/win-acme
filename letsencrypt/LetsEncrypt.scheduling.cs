using letsencrypt.Support;
using Microsoft.Win32.TaskScheduler;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace letsencrypt
{
    public partial class LetsEncrypt
    {
        public static void ScheduleRenewal(Target target, Options options)
        {
            Settings settings = new Settings(options.ConfigPath);

            EnsureTaskScheduler(settings, options);

            Log.Information(R.Addingrenewalfortarget, target);
            var renewals = settings.Renewals;

            foreach (var existing in from r in renewals.ToArray() where r.Binding.Host == target.Host select r)
            {
                Log.Information(R.Removingexistingscheduledrenewal, existing);
                renewals.Remove(existing);
            }

            var result = new ScheduledRenewal
            {
                Binding = target,
                CentralSsl = options.CentralSslStore,
                San = options.San.ToString(),
                Date = DateTime.UtcNow.AddDays(options.RenewalPeriod),
                KeepExisting = options.KeepExisting.ToString(),
                Script = options.Script,
                ScriptParameters = options.ScriptParameters,
                Warmup = options.Warmup
            };
            renewals.Add(result);
            settings.Save();

            Log.Information(R.Renewalscheduledresult, result);
        }

        public static string CleanFileName(string fileName)
        {
            return
                Path.GetInvalidFileNameChars()
                    .Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        internal static void EnsureTaskScheduler(Settings settings, Options options)
        {
            var taskName = CLIENT_NAME + " " + CleanFileName(options.BaseUri);

            using (var taskService = new TaskService())
            {
                if (settings.ScheduledTaskName == taskName)
                {
                    if (!PromptYesNo(options, string.Format("\n" + R.DoyouwanttoreplacetheexistingtaskName, taskName), false))
                    {
                        return;
                    }
                    Log.Information(R.DeletingexistingtaskNamefromWindowsTaskScheduler, taskName);
                    taskService.RootFolder.DeleteTask(taskName, false);
                }
                
                Log.Information(R.CreatingtaskNamewithWindowsTaskScheduler, taskName);

                // Create a new task definition and assign properties
                var task = taskService.NewTask();
                task.RegistrationInfo.Description = R.CheckforrenewalofACMEcertificates;

                var now = DateTime.Now;
                var runtime = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0);
                task.Triggers.Add(new DailyTrigger { DaysInterval = 1, StartBoundary = runtime });

                var currentExec = Assembly.GetExecutingAssembly().Location;

                // Create an action that will launch the app with the renew parameters whenever the trigger fires
                string actionString = $"--renew --baseuri \"{options.BaseUri}\"";
                if (!string.IsNullOrWhiteSpace(options.CertOutPath))
                {
                    actionString += $" --certoutpath \"{options.CertOutPath}\"";
                }
                task.Actions.Add(new ExecAction(currentExec, actionString, Path.GetDirectoryName(currentExec)));

                task.Principal.RunLevel = TaskRunLevel.Highest; // need admin

                if (!options.Silent && !options.UseDefaultTaskUser && PromptYesNo(options, "\n" + R.Doyouwanttospecifytheuserthetaskwillrunas, false))
                {
                    // Ask for the login and password to allow the task to run
                    var username = PromptForText(options, R.Entertheusername);
                    var password = PromptForText(options, R.Entertheuserspassword);
                    taskService.RootFolder.RegisterTaskDefinition(taskName, task, TaskCreation.Create, username, password.ToString(), TaskLogonType.Password);
                }
                else
                {
                    Log.Information(R.Creatingtasktorunascurrentuseronlywhentheuserisloggedon);
                    taskService.RootFolder.RegisterTaskDefinition(taskName, task);
                }
                settings.ScheduledTaskName = taskName;
            }
        }
    }
}
