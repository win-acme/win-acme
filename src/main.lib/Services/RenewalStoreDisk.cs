using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services
{
    internal class RenewalStoreDisk : RenewalStore
    {
        public RenewalStoreDisk(
            ISettingsService settings, ILogService log, 
            IInputService input, PasswordGenerator password, IDueDateService dueDate,
            IPluginService plugin, ICacheService certificateService) :
            base(settings, log, input, password, plugin, dueDate, certificateService) { }

        /// <summary>
        /// Local cache to prevent superfluous reading and
        /// JSON parsing
        /// </summary>
        internal List<Renewal>? _renewalsCache;

        /// <summary>
        /// Parse renewals from store
        /// </summary>
        protected override IEnumerable<Renewal> ReadRenewals()
        {
            if (_renewalsCache == null)
            {
                var list = new List<Renewal>();
                var di = new DirectoryInfo(_settings.Client.ConfigurationPath);
                var postFix = ".renewal.json";
                var renewalFiles = di.EnumerateFiles($"*{postFix}", SearchOption.AllDirectories);
                foreach (var rj in renewalFiles)
                {
                    try
                    {
                        // Just checking if we have write permission
                        using var writeStream = rj.OpenWrite();
                    }
                    catch (Exception ex)
                    {
                        _log.Warning("No write access to all renewals: {reason}", ex.Message);
                        break;
                    }
                }
                foreach (var rj in renewalFiles)
                {
                    try
                    {
                        var storeConverter = new PluginOptionsConverter<StorePluginOptions>(_plugin.PluginOptionTypes<StorePluginOptions>(), _log);
                        var text = File.ReadAllText(rj.FullName);
                        var options = new JsonSerializerOptions();
                        options.PropertyNameCaseInsensitive = true;
                        options.Converters.Add(new ProtectedStringConverter(_log, _settings)); 
                        options.Converters.Add(new StoresPluginOptionsConverter(storeConverter));
                        options.Converters.Add(new PluginOptionsConverter<TargetPluginOptions>(_plugin.PluginOptionTypes<TargetPluginOptions>(), _log));
                        options.Converters.Add(new PluginOptionsConverter<CsrPluginOptions>(_plugin.PluginOptionTypes<CsrPluginOptions>(), _log));
                        options.Converters.Add(new PluginOptionsConverter<OrderPluginOptions>(_plugin.PluginOptionTypes<OrderPluginOptions>(), _log));
                        options.Converters.Add(new PluginOptionsConverter<ValidationPluginOptions>(_plugin.PluginOptionTypes<ValidationPluginOptions>(), _log));
                        options.Converters.Add(new PluginOptionsConverter<InstallationPluginOptions>(_plugin.PluginOptionTypes<InstallationPluginOptions>(), _log));
                        var result = JsonSerializer.Deserialize<Renewal>(text, options);
                        if (result == null)
                        {
                            throw new Exception("result is empty");
                        }
                        if (result.Id != rj.Name.Replace(postFix, ""))
                        {
                            throw new Exception($"mismatch between filename and id {result.Id}");
                        }
                        if (result.TargetPluginOptions == null)
                        {
                            throw new Exception("missing TargetPluginOptions");
                        }
                        if (result.TargetPluginOptions.GetType() == typeof(TargetPluginOptions))
                        {
                            throw new Exception($"missing source plugin {result.TargetPluginOptions.Plugin}");
                        }
                        if (result.ValidationPluginOptions == null)
                        {
                            throw new Exception("missing ValidationPluginOptions");
                        }
                        if (result.ValidationPluginOptions.GetType() == typeof(ValidationPluginOptions))
                        {
                            throw new Exception($"missing validation plugin {result.ValidationPluginOptions.Plugin}");
                        }
                        if (result.StorePluginOptions == null)
                        {
                            throw new Exception("missing StorePluginOptions");
                        }
                        if (result.CsrPluginOptions == null && result.TargetPluginOptions.Name != CsrOptions.NameLabel)
                        {
                            throw new Exception("missing CsrPluginOptions");
                        }
                        if (result.InstallationPluginOptions == null)
                        {
                            throw new Exception("missing InstallationPluginOptions");
                        }
                        if (string.IsNullOrEmpty(result.LastFriendlyName))
                        {
                            result.LastFriendlyName = result.FriendlyName;
                        }
                        if (result.History == null)
                        {
                            result.History = new List<RenewResult>();
                        }
                        list.Add(result);
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Unable to read renewal {renewal}: {reason}", rj.Name, ex.Message);
                    }
                }
                _renewalsCache = list.OrderBy(x => _dueDateService.DueDate(x)).ToList();
            }
            return _renewalsCache;
        }

        /// <summary>
        /// Serialize renewal information to store
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <param name="Renewals"></param>
        protected override void WriteRenewals(IEnumerable<Renewal> Renewals)
        {
            var list = Renewals.ToList();
            list.ForEach(renewal =>
            {
                if (renewal.Deleted)
                {
                    var file = RenewalFile(renewal, _settings.Client.ConfigurationPath);
                    if (file != null && file.Exists)
                    {
                        file.Delete();
                    }
                }
                else if (renewal.Updated || renewal.New)
                {
                    var file = RenewalFile(renewal, _settings.Client.ConfigurationPath);
                    if (file != null)
                    {
                        try
                        {
                            var options = new JsonSerializerOptions();
                            options.WriteIndented = true;
                            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                            options.Converters.Add(new ProtectedStringConverter(_log, _settings));
                            var renewalContent = JsonSerializer.Serialize(renewal, options);
                            if (string.IsNullOrWhiteSpace(renewalContent))
                            {
                                throw new Exception("Serialization yielded empty result");
                            }
                            if (file.Exists)
                            {
                                File.WriteAllText(file.FullName + ".new", renewalContent);
                                File.Replace(file.FullName + ".new", file.FullName, file.FullName + ".previous", true);
                                File.Delete(file.FullName + ".previous");
                            } 
                            else
                            {
                                File.WriteAllText(file.FullName, renewalContent);
                            }

                        } 
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Unable to write {renewal} to disk", renewal.LastFriendlyName);
                        }
                    }
                    renewal.New = false;
                    renewal.Updated = false;
                }
            });
            _renewalsCache = list.Where(x => !x.Deleted).OrderBy(x => _dueDateService.DueDate(x)).ToList();
        }

        /// <summary>
        /// Determine location and name of the history file
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="configPath"></param>
        /// <returns></returns>
        private static FileInfo RenewalFile(Renewal renewal, string configPath) => new(Path.Combine(configPath, $"{renewal.Id}.renewal.json"));
    }
}
