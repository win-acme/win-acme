using Autofac.Core;
using Fclp.Internals.Extensions;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
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
        private readonly IInputService _input;
        private readonly ILogService _log;
        private readonly DueDateStaticService _dueDate;

        public RenewalDescriber(
            ISharingLifetimeScope container,
            IPluginService plugin,
            ISettingsService settings,
            IInputService input,
            ILogService log,
            DueDateStaticService dueDate,
            IAutofacBuilder autofacBuilder)
        {
            _settings = settings;
            _container = container;
            _scopeBuilder = autofacBuilder;
            _log = log;
            _plugin = plugin;
            _input = input;
            _dueDate = dueDate;
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
            if (!string.IsNullOrWhiteSpace(renewal.FriendlyName))
            {
                args.Add("friendlyname", renewal.FriendlyName);
            }
            if (!string.IsNullOrWhiteSpace(renewal.Account))
            {
                args.Add("account", renewal.Account);
            }
            return "wacs.exe " + string.Join(" ", args.Select(a => $"--{a.Key.ToLower()} {a.Value}".Trim()));
        }

        /// <summary>
        /// Escape command line argument
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string Escape(string value)
        {
            if (value.Contains(' ') || value.Contains('"'))
            {
                return $"\"{value.Replace("\"", "\\\"")}\"";
            }
            return value;
        }

        /// <summary>
        /// Show renewal details on screen
        /// </summary>
        /// <param name="renewal"></param>
        public void Show(Renewal renewal)
        {
            try
            {
                _input.CreateSpace();
                _input.Show("Id", renewal.Id);
                _input.Show("File", $"{renewal.Id}.renewal.json");
                if (string.IsNullOrWhiteSpace(renewal.Account))
                {
                    _input.Show("Account", "Default account");
                }
                else
                {
                    _input.Show("Account", $"Named account: {renewal.Account}");
                }
                if (string.IsNullOrWhiteSpace(renewal.FriendlyName))
                {
                    _input.Show("Auto-FriendlyName", renewal.LastFriendlyName);
                }
                else
                {
                    _input.Show("FriendlyName", renewal.FriendlyName);
                }
                _input.Show(".pfx password", renewal.PfxPassword?.Value);
                var expires = renewal.History.Where(x => x.Success == true).LastOrDefault()?.ExpireDate;
                if (expires == null)
                {
                    _input.Show("Expires", "Unknown");
                }
                else
                {
                    _input.Show("Expires", _input.FormatDate(expires.Value));
                }
                var dueDate = _dueDate.DueDate(renewal);
                if (dueDate == null)
                {
                    _input.Show("Renewal due", "Now");
                }
                else
                {
                    if (dueDate.Start != dueDate.End)
                    {
                        _input.Show("Renewal due start", _input.FormatDate(dueDate.Start));
                        _input.Show("Renewal due end", _input.FormatDate(dueDate.End));
                    }
                    else
                    {
                        _input.Show("Renewal due", _input.FormatDate(dueDate.End));
                    }
                }
                _input.Show("Renewed", $"{renewal.History.Where(x => x.Success == true).Count()} times");
                _input.Show("Command", Describe(renewal));

                _input.CreateSpace();
                _input.Show($"Plugins");
                _input.CreateSpace();

                renewal.TargetPluginOptions.Show(_input, _plugin);
                renewal.ValidationPluginOptions.Show(_input, _plugin);
                renewal.OrderPluginOptions?.Show(_input, _plugin);
                renewal.CsrPluginOptions?.Show(_input, _plugin);
                foreach (var ipo in renewal.StorePluginOptions)
                {
                    ipo.Show(_input, _plugin);
                }
                foreach (var ipo in renewal.InstallationPluginOptions)
                {
                    ipo.Show(_input, _plugin);
                }
                _input.CreateSpace();
                _input.Show($"Orders");
                _input.CreateSpace();
                var orders = _dueDate.CurrentOrders(renewal);
                var i = 0;
                foreach (var order in orders)
                {
                    _input.Show($"Order {++i}/{orders.Count}", order.Key);
                    _input.Show($"Renewed", $"{order.RenewCount} times", 1);
                    _input.Show($"Last thumbprint", order.LastThumbprint, 1);
                    if (order.LastRenewal != null)
                    {
                        _input.Show($"Last date", _input.FormatDate(order.LastRenewal.Value), 1);
                    }
                    var orderDue = order.DueDate;
                    if (orderDue.Start != orderDue.End)
                    {
                        _input.Show("Next start", _input.FormatDate(orderDue.Start), 1);
                        _input.Show("Next end", _input.FormatDate(orderDue.End), 1);
                    }
                    else
                    {
                        _input.Show("Next due", _input.FormatDate(orderDue.End), 1);
                    }
                    if (order.Revoked)
                    {
                        _input.Show($"Revoked", "true", 1);
                    }
                    _input.CreateSpace();
                }

                _input.Show($"History");
                _input.CreateSpace();

                var historyLimit = 10;
                var h = renewal.History.Count;
                foreach (var history in renewal.History.AsEnumerable().Reverse().Take(historyLimit))
                {
                    _input.Show($"History {h--}/{renewal.History.Count}");
                    _input.Show($"Date", _input.FormatDate(history.Date), 1);
                    foreach (var order in history.OrderResults)
                    {
                        _input.Show($"Order", order.Name, 1);
                        if (order.Success == true)
                        {
                            _input.Show($"Success", "true", 2);
                            _input.Show($"Thumbprint", order.Thumbprint, 2);
                        }
                        if (order.Missing == true)
                        {
                            _input.Show($"Missing", "true", 2);
                        }
                        if (order.Revoked == true)
                        {
                            _input.Show($"Revoked", "true", 2);
                        }
                        if (order.ErrorMessages != null && order.ErrorMessages.Any())
                        {
                            _input.Show($"Errors", string.Join(", ", order.ErrorMessages.Select(x => x.ReplaceNewLines())), 2);
                        }
                    }
                    _input.CreateSpace();
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to list details for target");
            }
        }
    }
}