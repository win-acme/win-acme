using Autofac.Core;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS
{
    internal class RenewalDescriber
    {
        private readonly IPluginService _plugin;
        private readonly ISharingLifetimeScope _container;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ISettingsService _settings;

        public RenewalDescriber(
            ISharingLifetimeScope container,
            IPluginService plugin,
            ISettingsService settings,
            IAutofacBuilder autofacBuilder)
        {
            _settings = settings;
            _container = container;
            _scopeBuilder = autofacBuilder;
            _plugin = plugin;
        }

        /// <summary>
        /// Write the command line that can be used to create 
        /// </summary>
        /// <param name="renewal"></param>
        public string Describe(Renewal renewal)
        {
            // List the command line that may be used to (re)create this renewal
            var args = new Dictionary<string, string?>();
            var addArgs = (PluginOptions p) =>
            {
                var arguments = p.Describe(_container, _scopeBuilder, _plugin);
                foreach (var arg in arguments)
                {
                    var meta = arg.Key;
                    if (!args.ContainsKey(meta.ArgumentName))
                    {
                        var value = arg.Value;
                        if (value != null)
                        {
                            var add = true;
                            if (value is ProtectedString protectedString)
                            {
                                value = protectedString.Value?.StartsWith(SecretServiceManager.VaultPrefix) ?? false ? protectedString.Value : (object)"*******";
                            }
                            else if (value is string singleString)
                            {
                                value = meta.Secret ? "*******" : Escape(singleString);
                            }
                            else if (value is List<string> listString)
                            {
                                value = Escape(string.Join(",", listString));
                            }
                            else if (value is List<int> listInt)
                            {
                                value = string.Join(",", listInt);
                            }
                            else if (value is List<long> listLong)
                            {
                                value = string.Join(",", listLong);
                            }
                            else if (value is bool boolean)
                            {
                                value = boolean ? null : add = false;
                            }
                            if (add)
                            {
                                args.Add(meta.ArgumentName, value?.ToString());
                            }
                        }
                    }
                }
            };

            args.Add("source", _plugin.GetPlugin(renewal.TargetPluginOptions).Name.ToLower());
            addArgs(renewal.TargetPluginOptions);
            var validationPlugin = _plugin.GetPlugin(renewal.ValidationPluginOptions);
            var validationName = validationPlugin.Name.ToLower();
            if (!string.Equals(validationName, _settings.Validation.DefaultValidation ?? "selfhosting", StringComparison.OrdinalIgnoreCase))
            {
                args.Add("validation", validationName);
            }
            addArgs(renewal.ValidationPluginOptions);
            if (renewal.OrderPluginOptions != null)
            {
                var orderName = _plugin.GetPlugin(renewal.OrderPluginOptions).Name.ToLower();
                if (!string.Equals(orderName, _settings.Order.DefaultPlugin ?? "single", StringComparison.OrdinalIgnoreCase))
                {
                    args.Add("order", orderName);
                }
                addArgs(renewal.OrderPluginOptions);
            }
            if (renewal.CsrPluginOptions != null)
            {
                var csrName = _plugin.GetPlugin(renewal.CsrPluginOptions).Name.ToLower();
                if (!string.Equals(csrName, _settings.Csr.DefaultCsr ?? "rsa", StringComparison.OrdinalIgnoreCase))
                {
                    args.Add("csr", csrName);
                }
                addArgs(renewal.CsrPluginOptions);
            }
            var storeNames = string.Join(",", renewal.StorePluginOptions.Select(_plugin.GetPlugin).Select(x => x.Name.ToLower()));
            if (!string.Equals(storeNames, _settings.Store.DefaultStore ?? "certificatestore", StringComparison.OrdinalIgnoreCase))
            {
                args.Add("store", storeNames);
            }
            foreach (var so in renewal.StorePluginOptions)
            {
                addArgs(so);
            }
            var installationNames = string.Join(",", renewal.InstallationPluginOptions.Select(_plugin.GetPlugin).Select(x => x.Name.ToLower()));
            if (!string.Equals(installationNames, _settings.Installation.DefaultInstallation ?? "none", StringComparison.OrdinalIgnoreCase))
            {
                args.Add("installation", installationNames);
            }
            foreach (var so in renewal.InstallationPluginOptions)
            {
                addArgs(so);
            }
            if (renewal.FriendlyName != null)
            {
                args.Add("friendlyname", renewal.FriendlyName);
            }
            return "wacs.exe " + string.Join(" ", args.Select(a => $"--{a.Key.ToLower()} {a.Value}".Trim()));
        }

        private static string Escape(string value)
        {
            if (value.Contains(' ') || value.Contains('"'))
            {
                return $"\"{value.Replace("\"", "\\\"")}\"";
            }
            return value;
        }
    }
}
