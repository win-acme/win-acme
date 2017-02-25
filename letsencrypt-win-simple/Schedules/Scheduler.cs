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

        // Replaces the characters of the typed in password with asterisks
        // More info: http://rajeshbailwal.blogspot.com/2012/03/password-in-c-console-application.html
        private static String ReadPassword()
        {
            var password = new StringBuilder();
            try
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                while (info.Key != ConsoleKey.Enter)
                {
                    if (info.Key != ConsoleKey.Backspace)
                    {
                        Console.Write("*");
                        password.Append(info.KeyChar);
                    }
                    else if (info.Key == ConsoleKey.Backspace)
                    {
                        if (password != null)
                        {
                            // remove one character from the list of password characters
                            password.Remove(password.Length - 1, 1);
                            // get the location of the cursor
                            int pos = Console.CursorLeft;
                            // move the cursor to the left by one character
                            Console.SetCursorPosition(pos - 1, Console.CursorTop);
                            // replace it with space
                            Console.Write(" ");
                            // move the cursor to the left by one character again
                            Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        }
                    }
                    info = Console.ReadKey(true);
                }
                // add a new line because user pressed enter at the end of their password
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Log.Error("Error Reading Password: {@ex}", ex);
            }

            return password.ToString();
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
