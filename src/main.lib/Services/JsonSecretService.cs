using Newtonsoft.Json;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Services
{
    /// <summary>
    /// Save secrets to a JSON file in the configuration folder, protected by ProtectedStrings
    /// </summary>
    class JsonSecretService : ISecretService
    {
        private readonly FileInfo _file;
        private readonly List<CredentialEntry> _secrets;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private const string _prefix = "vault-json-";
        private const string _filename = "secrets.json";

        /// <summary>
        /// Make references to this provider unique from 
        /// references in other providers
        /// </summary>
        public string Prefix => "json";

        /// <summary>
        /// Initial parsing of the file
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        public JsonSecretService(ISettingsService settings, ILogService log)
        {
            _log = log;
            _settings = settings;
            _file = new FileInfo(Path.Join(settings.Client.ConfigurationPath, _filename));
            _secrets = new List<CredentialEntry>();
            if (_file.Exists)
            {
                var parsed = JsonConvert.DeserializeObject<List<CredentialEntry>>(
                File.ReadAllText(_file.FullName),
                    new JsonSerializerSettings()
                    {
                        ObjectCreationHandling = ObjectCreationHandling.Replace,
                        Converters = { new ProtectedStringConverter(_log, _settings) }
                    });
                if (parsed == null)
                {
                    _log.Error("Unable to parse {filename}", _filename);
                }
                else
                {
                    _secrets = parsed;
                    _log.Debug("Found {x} secrets in {filename}", parsed.Count, _filename);
                }
            }
            else
            {
                _log.Debug("{filename} not found", _filename);
            }
        }

        /// <summary>
        /// Read secret from the file
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public string? GetSecret(string? identifier)
        { 
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return identifier;
            }
            return _secrets.FirstOrDefault(x => string.Equals(x.Key, identifier, StringComparison.OrdinalIgnoreCase))?.Secret?.Value ?? identifier;
        }

        /// <summary>
        /// Add or overwrite secret, return the key to store
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="secret"></param>
        public void PutSecret(string identifier, string secret)
        {
            var existing = _secrets.FirstOrDefault(x => x.Key == identifier);
            if (existing != null)
            {
                existing.Secret = new ProtectedString(secret);
            } 
            else
            {
                _secrets.Add(new CredentialEntry()
                {
                    Key = identifier,
                    Secret = new ProtectedString(secret)
                });
            }
            Save();
        }

        /// <summary>
        /// Save files back to JSON
        /// </summary>
        public void Save()
        {
            var newData = JsonConvert.SerializeObject(_secrets, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                Converters = { new ProtectedStringConverter(_log, _settings) }
            });
            if (newData != null)
            {
                if (_file.Exists)
                {
                    File.WriteAllText(_file.FullName + ".new", newData);
                    File.Replace(_file.FullName + ".new", _file.FullName, _file.FullName + ".previous", true);
                    File.Delete(_file.FullName + ".previous");
                }
                else
                {
                    File.WriteAllText(_file.FullName, newData);
                }
            }
        }

        /// <summary>
        /// Interal data storage format
        /// </summary>
        class CredentialEntry
        {
            public string? Key { get; set; }
            public ProtectedString? Secret { get; set; }
        }
    }
}
