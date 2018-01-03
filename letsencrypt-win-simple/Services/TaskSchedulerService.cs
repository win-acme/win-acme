using Microsoft.Win32.TaskScheduler;
using System;
using System.Reflection;
using LetsEncrypt.ACME.Simple.Extensions;
using System.IO;
using System.Runtime.InteropServices;

namespace LetsEncrypt.ACME.Simple.Services
{
    class TaskSchedulerService
    {
        private Options _options;
        private ISettingsService _settings;
        private IInputService _input;
        private ILogService _log;
        private string _clientName;

        public TaskSchedulerService(ISettingsService settings, IOptionsService options, IInputService input, ILogService log, string clientName)
        {
            _options = options.Options;
            _settings = settings;
            _input = input;
            _log = log;
            _clientName = clientName;
        }

        public void EnsureTaskScheduler()
        {
            var taskName = $"{_clientName} {_options.BaseUri.CleanFileName()}";
            using (var taskService = new TaskService())
            {
                var existingTask = taskService.GetTask(taskName);
                if (existingTask != null)
                {
                    if (!string.IsNullOrEmpty(_options.Plugin) || !_input.PromptYesNo($"Do you want to replace the existing task?"))
                        return;

                    _log.Information("Deleting existing task {taskName} from Windows Task Scheduler.", taskName);
                    taskService.RootFolder.DeleteTask(taskName, false);
                }

                _log.Information("Creating task {taskName} with Windows Task scheduler at 9am every day.", taskName);

                // Create a new task definition and assign properties
                var task = taskService.NewTask();
                task.RegistrationInfo.Description = "Check for renewal of ACME certificates.";

                var now = DateTime.Now;
                var runtime = new DateTime(now.Year, now.Month, now.Day, _settings.ScheduledTaskHour, 0, 0);
                task.Triggers.Add(new DailyTrigger { DaysInterval = 1, StartBoundary = runtime });
                task.Settings.ExecutionTimeLimit = new TimeSpan(2, 0, 0);
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.StopIfGoingOnBatteries = false;
                task.Settings.StartWhenAvailable = true;

                var currentExec = Assembly.GetExecutingAssembly().Location;

                // Create an action that will launch the app with the renew parameters whenever the trigger fires
                string actionString = $"--{nameof(Options.Renew).ToLowerInvariant()} --{nameof(Options.BaseUri).ToLowerInvariant()} \"{_options.BaseUri}\"";

                task.Actions.Add(new ExecAction(currentExec, actionString, Path.GetDirectoryName(currentExec)));
                task.Principal.RunLevel = TaskRunLevel.Highest; // need admin
                //Log.Debug("{@task}", task);

                while (true)
                {
                    try
                    {
                        if (!_options.UseDefaultTaskUser && _input.PromptYesNo($"Do you want to specify the user the task will run as?"))
                        {
                            // Ask for the login and password to allow the task to run 
                            var username = _input.RequestString("Enter the username (Domain\\username)");
                            var password = _input.ReadPassword("Enter the user's password");
                            _log.Debug("Creating task to run as {username}", username);
                            taskService.RootFolder.RegisterTaskDefinition(
                                taskName,
                                task,
                                TaskCreation.Create,
                                username,
                                password,
                                TaskLogonType.Password);
                        }
                        else if (existingTask != null)
                        {
                            _log.Debug("Creating task to run with previously chosen credentials");
                            string password = null;
                            string username = null;
                            if (existingTask.Definition.Principal.LogonType == TaskLogonType.Password)
                            {
                                username = existingTask.Definition.Principal.UserId;
                                password = _input.ReadPassword($"Password for {username}");
                            }
                            task.Principal.UserId = existingTask.Definition.Principal.UserId;
                            task.Principal.LogonType = existingTask.Definition.Principal.LogonType;
                            taskService.RootFolder.RegisterTaskDefinition(
                                taskName,
                                task,
                                TaskCreation.CreateOrUpdate,
                                username,
                                password,
                                existingTask.Definition.Principal.LogonType);
                        }
                        else
                        {
                            _log.Debug("Creating task to run as system user");
                            task.Principal.UserId = "SYSTEM";
                            task.Principal.LogonType = TaskLogonType.ServiceAccount;
                            taskService.RootFolder.RegisterTaskDefinition(
                                taskName,
                                task,
                                TaskCreation.CreateOrUpdate,
                                null,
                                null,
                                TaskLogonType.ServiceAccount);
                        }
                        break;
                    }
                    catch (COMException cex)
                    {
                        if (cex.HResult == -2147023570)
                        {
                            _log.Warning("Invalid username/password, please try again");
                        }
                        else
                        {
                            _log.Error(cex, "Failed to create task");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Failed to create task");
                        break;
                    }
                }
            }
        }
    }
}
