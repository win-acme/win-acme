using Microsoft.Win32.TaskScheduler;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PKISharp.WACS.Services
{
    internal class TaskSchedulerService
    {
        private readonly MainArguments _arguments;
        private readonly ISettingsService _settings;
        private readonly IInputService _input;
        private readonly ILogService _log;
        private readonly VersionService _version;

        public TaskSchedulerService(
            ISettingsService settings,
            IArgumentsService arguments,
            IInputService input,
            ILogService log,
            VersionService version)
        {
            _arguments = arguments.MainArguments;
            _settings = settings;
            _input = input;
            _log = log;
            _version = version;
        }
        private string TaskName(string clientName) => $"{clientName} renew ({_settings.BaseUri.CleanUri()})";
        private string WorkingDirectory => Path.GetDirectoryName(_version.ExePath);
        private string ExecutingFile => Path.GetFileName(_version.ExePath);

        private Task? ExistingTask
        {
            get
            {
                using (var taskService = new TaskService())
                {
                    var taskName = TaskName(_settings.Client.ClientName);
                    var existingTask = taskService.GetTask(taskName);
                    if (existingTask != null)
                    {
                        return existingTask;
                    }
                }
                return null;
            }
        }

        public bool ConfirmTaskScheduler()
        {
            var existingTask = ExistingTask;
            if (existingTask != null)
            {
                return IsHealthy(existingTask);
            }
            else
            {
                _log.Warning("Scheduled task not configured yet");
                return false;
            }
        }

        private bool IsHealthy(Task task)
        {
            var healthy = true;
            if (!task.Definition.Actions.OfType<ExecAction>().Any(action => 
                string.Equals(action.Path.Trim('"'), _version.ExePath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(action.WorkingDirectory.Trim('"'), WorkingDirectory, StringComparison.OrdinalIgnoreCase)))
            {
                healthy = false;
                _log.Warning("Scheduled task points to different location for .exe and/or working directory");
            }
            if (!task.Enabled)
            {
                healthy = false;
                _log.Warning("Scheduled task is disabled");
            }
            if (healthy)
            {
                _log.Information("Scheduled task looks healthy");
                return true;
            }
            else
            {
                _log.Warning("Scheduled task exists but does not look healthy");
                return false;
            }
        }

        public async System.Threading.Tasks.Task EnsureTaskScheduler(RunLevel runLevel, bool offerRecreate)
        {
            string taskName;
            var existingTask = ExistingTask;

            taskName = existingTask != null ? 
                existingTask.Name : 
                TaskName(_settings.Client.ClientName);

            using var taskService = new TaskService();
            if (existingTask != null)
            {
                var healthy = IsHealthy(existingTask);
                var recreate = false;
                if (runLevel.HasFlag(RunLevel.Interactive))
                {
                    if (offerRecreate || !healthy)
                    {
                        recreate = await _input.PromptYesNo($"Do you want to replace the existing task?", false);
                    } 
                }

                if (!recreate)
                {
                    if (!healthy)
                    {
                        _log.Error("Proceeding with unhealthy scheduled task, automatic renewals may not work until this is addressed");
                    }
                    return;
                }
         
                _log.Information("Deleting existing task {taskName} from Windows Task Scheduler.", taskName);
                taskService.RootFolder.DeleteTask(taskName, false);
            }

            var actionString = $"--{nameof(MainArguments.Renew).ToLowerInvariant()} --{nameof(MainArguments.BaseUri).ToLowerInvariant()} \"{_settings.BaseUri}\"";

            _log.Information("Adding Task Scheduler entry with the following settings", taskName);
            _log.Information("- Name {name}", taskName);
            _log.Information("- Path {action}", WorkingDirectory);
            _log.Information("- Command {exec} {action}", ExecutingFile, actionString);
            _log.Information("- Start at {start}", _settings.ScheduledTask.StartBoundary);
            if (_settings.ScheduledTask.RandomDelay.TotalMinutes > 0)
            {
                _log.Information("- Random delay {delay}", _settings.ScheduledTask.RandomDelay);
            }
            _log.Information("- Time limit {limit}", _settings.ScheduledTask.ExecutionTimeLimit);

            // Create a new task definition and assign properties
            var task = taskService.NewTask();
            task.RegistrationInfo.Description = "Check for renewal of ACME certificates.";

            var now = DateTime.Now;
            var runtime = new DateTime(now.Year, now.Month, now.Day,
                _settings.ScheduledTask.StartBoundary.Hours,
                _settings.ScheduledTask.StartBoundary.Minutes,
                _settings.ScheduledTask.StartBoundary.Seconds);

            task.Triggers.Add(new DailyTrigger
            {
                DaysInterval = 1,
                StartBoundary = runtime,
                RandomDelay = _settings.ScheduledTask.RandomDelay
            });
            task.Settings.ExecutionTimeLimit = _settings.ScheduledTask.ExecutionTimeLimit;
            task.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
            task.Settings.RunOnlyIfNetworkAvailable = true;
            task.Settings.DisallowStartIfOnBatteries = false;
            task.Settings.StopIfGoingOnBatteries = false;
            task.Settings.StartWhenAvailable = true;

            // Create an action that will launch the app with the renew parameters whenever the trigger fires
            task.Actions.Add(new ExecAction(_version.ExePath, actionString, WorkingDirectory));

            task.Principal.RunLevel = TaskRunLevel.Highest;
            while (true)
            {
                try
                {
                    if (!_arguments.UseDefaultTaskUser &&
                        runLevel.HasFlag(RunLevel.Interactive | RunLevel.Advanced) &&
                        await _input.PromptYesNo($"Do you want to specify the user the task will run as?", false))
                    {
                        // Ask for the login and password to allow the task to run 
                        var username = await _input.RequestString("Enter the username (Domain\\username)");
                        var password = await _input.ReadPassword("Enter the user's password");
                        _log.Debug("Creating task to run as {username}", username);
                        try
                        {
                            taskService.RootFolder.RegisterTaskDefinition(
                                taskName,
                                task,
                                TaskCreation.Create,
                                username,
                                password,
                                TaskLogonType.Password);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _log.Error("Unable to register scheduled task, please run as administrator or equivalent");
                        }
                    }
                    else if (existingTask != null)
                    {
                        _log.Debug("Creating task to run with previously chosen credentials");
                        string? password = null;
                        string? username = null;
                        if (existingTask.Definition.Principal.LogonType == TaskLogonType.Password)
                        {
                            username = existingTask.Definition.Principal.UserId;
                            password = await _input.ReadPassword($"Password for {username}");
                        }
                        task.Principal.UserId = existingTask.Definition.Principal.UserId;
                        task.Principal.LogonType = existingTask.Definition.Principal.LogonType;
                        try
                        {
                            taskService.RootFolder.RegisterTaskDefinition(
                                taskName,
                                task,
                                TaskCreation.CreateOrUpdate,
                                username,
                                password,
                                existingTask.Definition.Principal.LogonType);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _log.Error("Unable to register scheduled task, please run as administrator or equivalent");
                        }
                    }
                    else
                    {
                        _log.Debug("Creating task to run as system user");
                        task.Principal.UserId = "SYSTEM";
                        task.Principal.LogonType = TaskLogonType.ServiceAccount;
                        try
                        {
                            taskService.RootFolder.RegisterTaskDefinition(
                                taskName,
                                task,
                                TaskCreation.CreateOrUpdate,
                                null,
                                null,
                                TaskLogonType.ServiceAccount);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _log.Error("Unable to register scheduled task, please run as administrator or equivalent");
                        }
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
