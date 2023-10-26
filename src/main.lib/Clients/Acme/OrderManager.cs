using ACMESharp.Protocol;
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
        private readonly DirectoryInfo _orderPath;
        private const string _orderFileExtension = "order.json";
        private const string _orderKeyExtension = "order.keys";

        public OrderManager(ILogService log, ISettingsService settings)
        {
            _log = log;
            _settings = settings;
            _orderPath = settings.Valid ? 
                new DirectoryInfo(Path.Combine(settings.Client.ConfigurationPath, "Orders")) : 
                new DirectoryInfo(Directory.GetCurrentDirectory());
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
        private static string CacheKey(Order order, string accountId)
        {
            // Check if we can reuse a cached order based on currently
            // active set of parameters and shape of 
            // the target.
            var cacheKeyBuilder = new StringBuilder();
            cacheKeyBuilder.Append(accountId);
            cacheKeyBuilder.Append(order.Target.CommonName);
            cacheKeyBuilder.Append(string.Join(',', order.Target.GetIdentifiers(true).OrderBy(x => x).Select(x => x.Value.ToLower())));
            _ = order.Target.UserCsrBytes != null ?
                cacheKeyBuilder.Append(Convert.ToBase64String(order.Target.UserCsrBytes.ToArray())) :
                cacheKeyBuilder.Append('-');
            _ = order.Renewal.CsrPluginOptions != null ?
                cacheKeyBuilder.Append(JsonSerializer.Serialize(order.Renewal.CsrPluginOptions, WacsJson.Insensitive.CsrPluginOptions)) :
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
        public async Task<AcmeOrderDetails?> GetOrCreate(Order order, AcmeClient client, RunLevel runLevel)
        {
            var cacheKey = CacheKey(order, client.Account.Details.Kid);
            if (_settings.Cache.ReuseDays > 0)
            {
                // Above conditional not only prevents us from reading a cached
                // order from disk, but also prevent the "KeyPath" property from
                // being set in the first place, which in turn prevents the
                // CsrPlugin from caching the private key on disk.
                if (string.IsNullOrWhiteSpace(order.KeyPath))
                {
                    order.KeyPath = Path.Combine(_orderPath.FullName, $"{cacheKey}.{_orderKeyExtension}");
                }
                var orderDetails = await GetFromCache(cacheKey, client, runLevel);
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
            }
            return await CreateOrder(cacheKey, client, order.Target);
        }

        /// <summary>
        /// Delete all relevant files from the order cache
        /// </summary>
        /// <param name="cacheKey"></param>
        private void DeleteFromCache(string cacheKey)
        {
            DeleteFile($"{cacheKey}.{_orderFileExtension}");
            DeleteFile($"{cacheKey}.{_orderKeyExtension}");
        }

        /// <summary>
        /// Delete a file from the order cache
        /// </summary>
        /// <param name="path"></param>
        private void DeleteFile(string path)
        {
            var fileInfo = new FileInfo(Path.Combine(_orderPath.FullName, path));
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
                _log.Debug("Deleted {fileInfo}", fileInfo.FullName);
            }
        }

        /// <summary>
        /// Get order from the cache
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task<AcmeOrderDetails?> GetFromCache(string cacheKey, AcmeClient client, RunLevel runLevel)
        {
            var existingOrder = FindRecentOrder(cacheKey);
            if (existingOrder == null)
            {
                _log.Verbose("No existing order found");
                return null;
            }

            if (runLevel.HasFlag(RunLevel.NoCache))
            {
                // Delete previously cached order
                // and previously cached key as well
                // to ensure that it won't be used
                _log.Warning("Cached order available but not used with --{switch} option.",
                    nameof(MainArguments.NoCache).ToLower());
                DeleteFromCache(cacheKey);
                return null;
            }

            try
            {
                _log.Debug("Refreshing cached order");
                existingOrder = await RefreshOrder(existingOrder, client);
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to refresh cached order: {ex}", ex.Message);
                DeleteFromCache(cacheKey);
                return null;
            }

            if (existingOrder.Payload.Status != AcmeClient.OrderValid &&
                existingOrder.Payload.Status != AcmeClient.OrderReady)
            {
                _log.Warning("Cached order has status {status}, discarding", existingOrder.Payload.Status);
                DeleteFromCache(cacheKey);
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
        private async Task<AcmeOrderDetails> RefreshOrder(AcmeOrderDetails order, AcmeClient client)
        {
            _log.Debug("Refreshing order...");
            if (order.OrderUrl == null) 
            {
                throw new InvalidOperationException("Missing order url");
            }
            var update = await client.GetOrderDetails(order.OrderUrl);
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
        private async Task<AcmeOrderDetails?> CreateOrder(string cacheKey, AcmeClient client, Target target)
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

                // Determine notAfter value (unsupported by Let's
                // Encrypt at this time, but should work at Sectigo
                // and possibly others
                var validDays = _settings.Order.DefaultValidDays;
                // Certificates use UTC 
                var now = DateTime.UtcNow; 
                // We don't want milliseconds/ticks
                var nowRound = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Kind);
                var notAfter = validDays != null ?
                    nowRound.AddDays(validDays.Value) : 
                    (DateTime?)null;

                // Create the order
                _log.Verbose("Creating order for identifiers: {identifiers} (notAfter: {notAfter})", identifiers.Select(x => x.Value), notAfter);
                var order = await client.CreateOrder(identifiers, notAfter);
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
                if (_settings.Cache.ReuseDays <= 0)
                {
                    return;
                }
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

        /// <summary>
        /// Encrypt or decrypt the cached private keys
        /// </summary>
        public void Encrypt()
        {
            foreach (var f in _orderPath.EnumerateFiles($"*.{_orderKeyExtension}"))
            {
                var x = new ProtectedString(File.ReadAllText(f.FullName), _log);
                _log.Information("Rewriting {x}", f.Name);
                File.WriteAllText(f.FullName, x.DiskValue(_settings.Security.EncryptConfig));
            }
        }

        /// <summary>
        /// Delete all orders from cache
        /// </summary>
        internal void ClearCache()
        {
            foreach (var f in _orderPath.EnumerateFiles($"*.*"))
            {
                f.Delete();
            }
        }
    }
}
