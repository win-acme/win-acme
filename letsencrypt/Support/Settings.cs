using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace letsencrypt.Support
{
    internal class Settings
    {
        private SettingsObject values;
        private string configPath;
        private string settingsPath
        {
            get
            {
                return Path.Combine(configPath, "settings.json");
            }
        }

        public string ScheduledTaskName
        {
            get { return values.ScheduledTaskName; }
            set { values.ScheduledTaskName = value; Save(); }
        }

        public List<ScheduledRenewal> Renewals
        {
            get { return values.Renewals; }
            set { values.Renewals = value; Save(); }
        }

        public void Save()
        {
            File.WriteAllText(settingsPath, JsonConvert.SerializeObject(values, Formatting.Indented));
        }

        public Settings(string path)
        {
            configPath = path;
            if (File.Exists(settingsPath))
            {
                values = JsonConvert.DeserializeObject<SettingsObject>(File.ReadAllText(settingsPath));
            }
            else
            {
                values = new SettingsObject();
            }
            if (values.Renewals == null)
            {
                values.Renewals = new List<ScheduledRenewal>();
            }
        }
    }

    internal class SettingsObject
    {
        public List<ScheduledRenewal> Renewals;
        public string ScheduledTaskName;
    }
}