using Microsoft.Win32.TaskScheduler;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Host.Services.Legacy;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Services.Legacy
{
    internal class LegacyTaskSchedulerService
    {
        private readonly MainArguments _options;
        private readonly LegacySettingsService _settings;
        private readonly ILogService _log;

        public LegacyTaskSchedulerService(LegacySettingsService settings, MainArguments main, ILogService log)
        {
            _options = main;
            _settings = settings;
            _log = log;
        }

        public void StopTaskScheduler()
        {
            using var taskService = new TaskService();
            var taskName = "";
            Task? existingTask = null;
            foreach (var clientName in _settings.ClientNames.AsEnumerable().Reverse())
            {
                taskName = $"{clientName} {CleanFileName(_options.BaseUri)}";
                existingTask = taskService.GetTask(taskName);
                if (existingTask != null)
                {
                    break;
                }
            }

            if (existingTask != null)
            {
                existingTask.Definition.Settings.Enabled = false;
                _log.Warning("Disable existing task {taskName} in Windows Task Scheduler to prevent duplicate renewals", taskName);
                taskService.RootFolder.RegisterTaskDefinition(taskName, existingTask.Definition, TaskCreation.CreateOrUpdate, null);
            }
        }

        public string CleanFileName(string fileName) => Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
    }
}
