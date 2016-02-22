using System.Collections.Generic;
using Microsoft.Win32;
namespace LetsEncrypt.ACME.Simple
{
    /// <summary>
    /// Helper class to save/load renewals from the registry
    /// </summary>
    public class Settings
    {
        #region Constants
        /// <summary>
        /// Registry Key name for the Scheduled Task Name
        /// </summary>
        const string scheduledTaskKeyName = "ScheduledTaskName";
        /// <summary>
        /// The 'Renewals' registry value name
        /// </summary>
        const string renewalsValueName = "Renewals";
        #endregion
        #region Properties
        /// <summary>
        /// The base registry key to use when retrieving the <see cref="scheduledTaskKeyName"/> and <see cref="renewalsValueName"/>.
        /// </summary>
        string registryKey { get; }
        /// <summary>
        /// The name of the scheduled task
        /// </summary>
        public string ScheduledTaskName
        {
            get { return Registry.GetValue(registryKey, scheduledTaskKeyName, null) as string; }
            set { Registry.SetValue(registryKey, scheduledTaskKeyName, value); }
        }
        #endregion
        #region .ctor()
        /// <summary>
        /// Initialiser that sets up the proper value for <see cref="registryKey"/>
        /// </summary>
        /// <param name="clientName">The name of the application</param>
        /// <param name="cleanBaseUri">The API url that was used</param>
        public Settings(string clientName, string cleanBaseUri)
        {
            registryKey = $"HKEY_CURRENT_USER\\Software\\{clientName}\\{cleanBaseUri}";
        }
        #endregion
        #region Saving/Loading of renewals
        /// <summary>
        /// Load existing renewals fromt the registry
        /// </summary>
        /// <returns>existing renewals</returns>
        public List<ScheduledRenewal> LoadRenewals()
        {
            var result = new List<ScheduledRenewal>();
            var values = Registry.GetValue(registryKey, renewalsValueName, null) as string[];
            if (values != null)
            {
                foreach (var renewal in values)
                {
                    result.Add(ScheduledRenewal.Load(renewal));
                }
            }
            return result;
        }
        /// <summary>
        /// Stores renewals in the registry
        /// </summary>
        /// <param name="renewals">The renewals to store in the registry</param>
        public void SaveRenewals(List<ScheduledRenewal> renewals)
        {
            var serialized = new List<string>();

            foreach (var renewal in renewals)
            {
                serialized.Add(renewal.Save());
            }

            Registry.SetValue(registryKey, renewalsValueName, serialized.ToArray());
        }
        #endregion
    }
}