using Microsoft.Win32.TaskScheduler;
using PKISharp.WACS.Configuration.Arguments;
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

        public TaskSchedulerService(
            ISettingsService settings,
            MainArguments arguments,
            IInputService input,
            ILogService log)
        {
            _arguments = arguments;
            _settings = settings;
            _input = input;
            _log = log;
        }
        private string TaskName => $"{_settings.Client.ClientName} renew ({_settings.BaseUri.CleanUri()})";
        private static string WorkingDirectory => Path.GetDirectoryName(VersionService.ExePath) ?? "";
        private static string ExecutingFile => Path.GetFileName(VersionService.ExePath);

        private Task? ExistingTask
        {
            get
            {
                using var taskService = new TaskService();
                return taskService.GetTask(TaskName);
            }
        }

        public bool ConfirmTaskScheduler()
        {
            try
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
            catch (Exception ex)
            {
                _log.Error(ex, "Scheduled task health check failed");
                return false;
            }
        }

        private bool IsHealthy(Task task)
        {
            var healthy = true;
            var action = task.Definition.Actions.OfType<ExecAction>().
                Where(action => string.Equals(action.Path?.Trim('"'), VersionService.ExePath, StringComparison.OrdinalIgnoreCase)).
                Where(action => string.Equals(action.WorkingDirectory?.Trim('"'), WorkingDirectory, StringComparison.OrdinalIgnoreCase)).
                FirstOrDefault();
            var trigger = task.Definition.Triggers.FirstOrDefault();
            if (action == null)
            {
                healthy = false;
                _log.Warning("Scheduled task points to different location for .exe and/or working directory");
            } 
            else
            {
                var filtered = action.Arguments.Replace("--verbose", "").Trim();
                if (!string.Equals(filtered, Arguments, StringComparison.OrdinalIgnoreCase))
                {
                    healthy = false;
                    _log.Warning("Scheduled task arguments do not match with expected value");
                }
            }
            if (trigger == null)
            {
                healthy = false;
                _log.Warning("Scheduled task doesn't have a trigger configured");
            }
            else
            {
                if (!trigger.Enabled)
                {
                    healthy = false;
                    _log.Warning("Scheduled task trigger is disabled");
                }
                if (trigger is DailyTrigger dt)
                {
                    if (dt.StartBoundary.TimeOfDay != _settings.ScheduledTask.StartBoundary)
                    {
                        healthy = false;
                        _log.Warning("Scheduled task start time mismatch");
                    }
                    if (dt.RandomDelay != _settings.ScheduledTask.RandomDelay)
                    {
                        healthy = false;
                        _log.Warning("Scheduled task random delay mismatch");
                    }
                } 
                else
                {
                    healthy = false;
                    _log.Warning("Scheduled task trigger is not daily");
                }
            }
            if (task.Definition.Settings.ExecutionTimeLimit != _settings.ScheduledTask.ExecutionTimeLimit)
            {
                healthy = false;
                _log.Warning("Scheduled task execution time limit mismatch");
            }
            if (!task.Enabled)
            {
                healthy = false;
                _log.Warning("Scheduled task is disabled");
            }

            // Report final result
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

        /// <summary>
        /// Arguments that are supposed to be passed to wacs.exe when the
        /// scheduled task runs
        /// </summary>
        private string Arguments => 
            $"--{nameof(MainArguments.Renew).ToLowerInvariant()} " +
            $"--{nameof(MainArguments.BaseUri).ToLowerInvariant()} " +
            $"\"{_settings.BaseUri}\"";

        /// <summary>
        /// Decide to (re)create scheduled task or not
        /// </summary>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task EnsureTaskScheduler(RunLevel runLevel)
        {
            var existingTask = ExistingTask;
            var create = existingTask == null;
            if (existingTask != null)
            {
                var healthy = IsHealthy(existingTask);
                if (!healthy)
                {
                    if (runLevel.HasFlag(RunLevel.Interactive))
                    {
                        create = await _input.PromptYesNo($"Do you want to replace the existing task?", false);
                    }
                    else
                    {
                        _log.Error("Proceeding with unhealthy scheduled task, automatic renewals may not work until this is addressed");
                    }
                }
            }
            if (create)
            {
                await CreateTaskScheduler(runLevel);
            }
        }

        /// <summary>
        /// (Re)create the scheduled task
        /// </summary>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task CreateTaskScheduler(RunLevel runLevel)
        {
            using var taskService = new TaskService();
            var existingTask = ExistingTask;
            if (existingTask != null)
            {
                _log.Information("Deleting existing task {taskName} from Windows Task Scheduler.", TaskName);
                taskService.RootFolder.DeleteTask(TaskName, false);
            }
          
            _log.Information("Adding Task Scheduler entry with the following settings", TaskName);
            _log.Information("- Name {name}", TaskName);
            _log.Information("- Path {action}", WorkingDirectory);
            _log.Information("- Command {exec} {action}", ExecutingFile, Arguments);
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
            var actionPath = VersionService.ExePath;
            if (actionPath.IndexOf(" ") > -1)
            {
                actionPath = $"\"{actionPath}\"";
            }
            var workingPath = WorkingDirectory;
            _ = task.Actions.Add(new ExecAction(actionPath, Arguments, workingPath));

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
                                TaskName,
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
                                TaskName,
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
                                TaskName,
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
