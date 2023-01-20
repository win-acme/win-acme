using ACMESharp.Protocol;
using Org.BouncyCastle.Crypto.Agreement;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients.Acme
{
    /// <summary>
    /// The OrderManager makes sure that we don't hit rate limits
    /// </summary>
    class OrderManager
    {
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly AcmeClient _client;
        private readonly DirectoryInfo _orderPath;
        private const string _orderFileExtension = "order.json";
        private const string _orderKeyExtension = "order.keys";

        public OrderManager(ILogService log, ISettingsService settings, AcmeClient client)
        {
            _log = log;
            _client = client;
            _settings = settings;
            _orderPath = new DirectoryInfo(Path.Combine(settings.Client.ConfigurationPath, "Orders"));
        }

        /// <summary>
        /// To check if it's possible to reuse a previously retrieved
        /// certificate we create a hash of its key properties and included
        /// that hash in the file name. If we get the same hash on a 
        /// subsequent run, it means it's safe to reuse (no relevant changes).
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static string CacheKey(Order order)
        {
            // Check if we can reuse a cached order based on currently
            // active set of parameters and shape of 
            // the target.
            var cacheKeyBuilder = new StringBuilder();
            cacheKeyBuilder.Append(order.Target.CommonName);
            cacheKeyBuilder.Append(string.Join(',', order.Target.GetIdentifiers(true).OrderBy(x => x).Select(x => x.Value.ToLower())));
            _ = order.Target.UserCsrBytes != null ?
                cacheKeyBuilder.Append(Convert.ToBase64String(order.Target.UserCsrBytes)) :
                cacheKeyBuilder.Append('-');
            _ = order.Renewal.CsrPluginOptions != null ?
                cacheKeyBuilder.Append(JsonSerializer.Serialize(order.Renewal.CsrPluginOptions, WacsJson.Default.CsrPluginOptions)) :
                cacheKeyBuilder.Append('-');
            cacheKeyBuilder.Append(order.KeyPath);
            return cacheKeyBuilder.ToString().SHA1();
        }

        /// <summary>
        /// Get a previously cached order or if its too old, create a new one
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public async Task<AcmeOrderDetails?> GetOrCreate(Order order, RunLevel runLevel)
        {
            var cacheKey = CacheKey(order);
            if (string.IsNullOrWhiteSpace(order.KeyPath))
            {
                order.KeyPath = Path.Combine(_orderPath.FullName, $"{cacheKey}.{_orderKeyExtension}");
            }
            var orderDetails = await GetFromCache(cacheKey, runLevel);
            if (orderDetails != null)
            {
                var keyFile = new FileInfo(order.KeyPath);
                if (keyFile.Exists)
                {
                    _log.Warning("Using cache. To force a new order within {days} days, " +
                          "run with --{switch}. Beware that you might run into rate limits.",
                          _settings.Cache.ReuseDays,
                          nameof(MainArguments.NoCache).ToLower());
                    return orderDetails;
                }
                else
                {
                    _log.Debug("Cached order available but not used.");
                }
            }
            return await CreateOrder(cacheKey, order.Target);
        }

        /// <summary>
        /// Get order from the cache
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task<AcmeOrderDetails?> GetFromCache(string cacheKey, RunLevel runLevel)
        {
            var existingOrder = FindRecentOrder(cacheKey);
            if (existingOrder == null)
            {
                _log.Verbose("No existing order found");
                return null;
            }

            if (runLevel.HasFlag(RunLevel.NoCache))
            {
                _log.Warning("Cached order available but not used with --{switch} option.",
                    nameof(MainArguments.NoCache).ToLower());
                return null;
            }

            try
            {
                _log.Debug("Refreshing cached order");
                existingOrder = await RefreshOrder(existingOrder);
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to refresh cached order: {ex}", ex.Message);
                return null;
            }

            if (existingOrder.Payload.Status != AcmeClient.OrderValid &&
                existingOrder.Payload.Status != AcmeClient.OrderReady)
            {
                _log.Warning("Cached order has status {status}, discarding", existingOrder.Payload.Status);
                return null;
            }
            
            // Make sure that the CsrBytes and PrivateKey are available
            // for this order
            return existingOrder;
        }

        /// <summary>
        /// Update order details from the server
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private async Task<AcmeOrderDetails> RefreshOrder(AcmeOrderDetails order)
        {
            _log.Debug("Refreshing order...");
            if (order.OrderUrl == null) 
            {
                throw new InvalidOperationException("Missing order url");
            }
            var update = await _client.GetOrderDetails(order.OrderUrl);
            order.Payload = update.Payload;
            return order;
        }

        /// <summary>
        /// Create new order at the server
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="csrPlugin"></param>
        /// <param name="privateKeyFile"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private async Task<AcmeOrderDetails?> CreateOrder(string cacheKey, Target target)
        {
            try
            {
                // Determine final shape of the certificate
                var identifiers = target.GetIdentifiers(false);
                var commonName = target.CommonName;
                if (!identifiers.Contains(commonName.Unicode(false)))
                {
                    _log.Warning($"Common name {commonName.Value} provided is invalid.");
                    commonName = identifiers.First();
                }

                // Create the order
                _log.Verbose("Creating order for hosts: {identifiers}", identifiers);
                var order = await _client.CreateOrder(identifiers);
                if (order.Payload.Error != default)
                {
                    _log.Error("Failed to create order {url}: {detail}", order.OrderUrl, order.Payload.Error.Detail);
                    return null;
                }
                
                _log.Verbose("Order {url} created", order.OrderUrl);
                await SaveOrder(order, cacheKey);
                return order;
            } 
            catch (AcmeProtocolException ex)
            {
                _log.Error($"Failed to create order: {ex.ProblemDetail ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to create order");
            }
            return null;
        }

        /// <summary>
        /// Check if we have a recent order that can be reused
        /// to prevent hitting rate limits
        /// </summary>
        /// <param name="identifiers"></param>
        /// <returns></returns>
        private AcmeOrderDetails? FindRecentOrder(string cacheKey)
        {
            DeleteStaleFiles();
            var fi = new FileInfo(Path.Combine(_orderPath.FullName, $"{cacheKey}.{_orderFileExtension}"));
            if (!fi.Exists || !IsValid(fi))
            {
                return null;
            }
            try
            {
                var content = File.ReadAllText(fi.FullName);
                var order = JsonSerializer.Deserialize(content, AcmeClientJson.Insensitive.AcmeOrderDetails);
                return order;
            } 
            catch (Exception ex)
            {
                _log.Warning("Unable to read order cache: {ex}", ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Delete files that are not valid anymore
        /// </summary>
        private void DeleteStaleFiles()
        {
            if (_orderPath.Exists)
            {
                var orders = new[] { 
                    $"*.{_orderFileExtension}",
                    $"*.{_orderKeyExtension}"
                }.SelectMany(_orderPath.EnumerateFiles);
                foreach (var order in orders)
                {
                    if (!IsValid(order))
                    {
                        try
                        {
                            order.Delete();
                        }
                        catch (Exception ex)
                        {
                            _log.Debug("Unable to clean up order cache: {ex}", ex.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Test if a cached order file is still usable
        /// </summary>
        /// <returns></returns>
        private bool IsValid(FileInfo order) => order.LastWriteTime > DateTime.Now.AddDays(_settings.Cache.ReuseDays * -1);

        /// <summary>
        /// Save order to disk
        /// </summary>
        /// <param name="order"></param>
        private async Task SaveOrder(AcmeOrderDetails order, string cacheKey)
        {
            try
            {
                if (!_orderPath.Exists)
                {
                    _orderPath.Create();
                }
                var content = JsonSerializer.Serialize(order, AcmeClientJson.Default.AcmeOrderDetails);
                var path = Path.Combine(_orderPath.FullName, $"{cacheKey}.{_orderFileExtension}");
                await File.WriteAllTextAsync(path, content);
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to write to order cache: {ex}", ex.Message);
            }
        }
    }
}
