using ACMESharp.Protocol;
using Newtonsoft.Json;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly ICertificateService _certificateService;
        private const string _orderFileExtension = "order.json";

        public OrderManager(ILogService log, ISettingsService settings, 
            AcmeClient client, ICertificateService certificateService)
        {
            _log = log;
            _client = client;
            _settings = settings;
            _certificateService = certificateService;
            _orderPath = new DirectoryInfo(Path.Combine(settings.Client.ConfigurationPath, "Orders"));
        }

        /// <summary>
        /// Get a previously cached order or if its too old, create a new one
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public async Task<OrderDetails?> GetOrCreate(Order order, RunLevel runLevel)
        {
            var cacheKey = _certificateService.CacheKey(order);
            var existingOrder = default(OrderDetails); // FindRecentOrder(cacheKey);
            if (existingOrder != null)
            {
                try
                {
                    if (runLevel.HasFlag(RunLevel.IgnoreCache))
                    {
                        _log.Warning("Cached order available but not used with the --{switch} switch.",
                            nameof(MainArguments.Force).ToLower());
                    }
                    else
                    {
                        existingOrder = await RefreshOrder(existingOrder);
                        if (existingOrder.Payload.Status == AcmeClient.OrderValid ||
                            existingOrder.Payload.Status == AcmeClient.OrderReady)
                        { 
                            _log.Warning("Using cached order. To force issue of a new certificate within {days} days, " +
                                "run with the --{switch} switch. Be ware that you might run into rate limits doing so.",
                                _settings.Cache.ReuseDays,
                                nameof(MainArguments.Force).ToLower());
                            return existingOrder;
                        }
                        else
                        {
                            _log.Debug("Cached order has status {status}, discarding", existingOrder.Payload.Status);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warning("Unable to refresh cached order: {ex}", ex.Message);
                }
            }
            var identifiers = order.Target.GetIdentifiers(false);
            return await CreateOrder(identifiers, cacheKey);
        }

        /// <summary>
        /// Update order details from the server
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private async Task<OrderDetails> RefreshOrder(OrderDetails order)
        {
            _log.Debug("Refreshing order...");
            var update = await _client.GetOrderDetails(order.OrderUrl);
            order.Payload = update.Payload;
            return order;
        }

        private async Task<OrderDetails?> CreateOrder(IEnumerable<Identifier> identifiers, string cacheKey)
        {
            _log.Verbose("Creating order for hosts: {identifiers}", identifiers);
            try
            {
                // TODO: modify AcmeSharp to understand
                // different types of identifier
                var order = await _client.CreateOrder(identifiers);
                if (order.Payload.Error != null)
                {
                    _log.Error("Failed to create order {url}: {detail}", order.OrderUrl, order.Payload.Error.Detail);
                    return null;
                }
                else
                {
                    _log.Verbose("Order {url} created", order.OrderUrl);
                    SaveOrder(order, cacheKey);
                }
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
        private OrderDetails? FindRecentOrder(string cacheKey)
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
                var order = JsonConvert.DeserializeObject<OrderDetails>(content);
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
                var orders = _orderPath.EnumerateFiles($"*.{_orderFileExtension}");
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
                            _log.Warning("Unable to clean up order cache: {ex}", ex.Message);
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
        private void SaveOrder(OrderDetails order, string cacheKey)
        {
            try
            {
                if (!_orderPath.Exists)
                {
                    _orderPath.Create();
                }
                var content = JsonConvert.SerializeObject(order);
                var path = Path.Combine(_orderPath.FullName, $"{cacheKey}.{_orderFileExtension}");
                File.WriteAllText(path, content);
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to write to order cache: {ex}", ex.Message);
            }
        }
    }
}
