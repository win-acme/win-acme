using Microsoft.Win32.TaskScheduler;
using System;
using System.Reflection;
using PKISharp.WACS.Extensions;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace PKISharp.WACS.Services
{
    class TaskSchedulerService
    {
        private Options _options;
        private SettingsService _settings;
        private IInputService _input;
        private ILogService _log;
        private RunLevel _runLevel;

        public TaskSchedulerService(
            SettingsService settings, 
            IOptionsService options,
            IInputService input, 
            ILogService log,
            RunLevel runLevel)
        {
            _options = options.Options;
            _settings = settings;
            _input = input;
            _log = log;
            _runLevel = runLevel;
        }

        public void EnsureTaskScheduler()
        {
            using (var taskService = new TaskService())
            {
                var taskName = "";
                Task existingTask = null;
                foreach (var clientName in _settings.ClientNames.Reverse())
                {
                    taskName = $"{clientName} {_options.BaseUri.CleanFileName()}";
                    existingTask = taskService.GetTask(taskName);
                    if (existingTask != null)
                    {
                        break;
                    }
                }
              
                if (existingTask != null)
                {
                    if (_runLevel != RunLevel.Advanced || !_input.PromptYesNo($"Do you want to replace the existing task?"))
                        return;

                    _log.Information("Deleting existing task {taskName} from Windows Task Scheduler.", taskName);
                    taskService.RootFolder.DeleteTask(taskName, false);
                }

                var currentExec = Assembly.GetExecutingAssembly().Location;
                string actionString = $"--{nameof(Options.Renew).ToLowerInvariant()} --{nameof(Options.BaseUri).ToLowerInvariant()} \"{_options.BaseUri}\"";

                _log.Information("Adding Task Scheduler entry with the following settings", taskName);
                _log.Information("- Name {name}", taskName);
                _log.Information("- Path {action}", Path.GetDirectoryName(currentExec));
                _log.Information("- Command {exec} {action}", Path.GetFileName(currentExec), actionString);
                _log.Information("- Start at {start}", _settings.ScheduledTaskStartBoundary);
                if (_settings.ScheduledTaskRandomDelay.TotalMinutes > 0)
                {
                    _log.Information("- Random delay {delay}", _settings.ScheduledTaskRandomDelay);
                }
                _log.Information("- Time limit {limit}", _settings.ScheduledTaskExecutionTimeLimit);

                // Create a new task definition and assign properties
                var task = taskService.NewTask();
                task.RegistrationInfo.Description = "Check for renewal of ACME certificates.";

                var now = DateTime.Now;
                var runtime = new DateTime(now.Year, now.Month, now.Day, 
                    _settings.ScheduledTaskStartBoundary.Hours, 
                    _settings.ScheduledTaskStartBoundary.Minutes,
                    _settings.ScheduledTaskStartBoundary.Seconds);

                task.Triggers.Add(new DailyTrigger {
                    DaysInterval = 1,
                    StartBoundary = runtime,
                    RandomDelay = _settings.ScheduledTaskRandomDelay
                });
                task.Settings.ExecutionTimeLimit = _settings.ScheduledTaskExecutionTimeLimit;
                task.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                task.Settings.RunOnlyIfNetworkAvailable = true;
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.StopIfGoingOnBatteries = false;
                task.Settings.StartWhenAvailable = true;

                // Create an action that will launch the app with the renew parameters whenever the trigger fires
                task.Actions.Add(new ExecAction(currentExec, actionString, Path.GetDirectoryName(currentExec)));

                task.Principal.RunLevel = TaskRunLevel.Highest; 
                while (true)
                {
                    try
                    {
                        if (!_options.UseDefaultTaskUser && 
                            _runLevel == RunLevel.Advanced && 
                            _input.PromptYesNo($"Do you want to specify the user the task will run as?"))
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
