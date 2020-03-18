using ACMESharp.Protocol;
using Newtonsoft.Json;
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
    class OrderManager
    {
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly AcmeClient _client;
        private readonly DirectoryInfo _orderPath;
        private readonly ICertificateService _certificateService;

        public OrderManager(ILogService log, ISettingsService settings, 
            AcmeClient client, ICertificateService certificateService)
        {
            _log = log;
            _client = client;
            _settings = settings;
            _certificateService = certificateService;
            _orderPath = new DirectoryInfo(Path.Combine(settings.Client.ConfigurationPath, "Orders"));
        }

        public async Task<OrderDetails?> GetOrCreate(Renewal renewal, Target target)
        {
            var cacheKey = _certificateService.CacheKey(renewal, target);
            var identifiers = target.GetHosts(false);
            var existingOrder = FindRecentOrder(identifiers, cacheKey);
            if (existingOrder != null)
            {
                try
                {
                    existingOrder = await RefreshOrder(existingOrder);
                    _log.Information("Reusing existing order");
                    return existingOrder;
                }
                catch (Exception ex)
                {
                    _log.Warning("Unable to refresh order: {ex}", ex.Message);
                }
            } 
            return await CreateOrder(identifiers, cacheKey);
        }

        private async Task<OrderDetails> RefreshOrder(OrderDetails order)
        {
            _log.Debug("Refreshing order...");
            var update = await _client.GetOrderDetails(order.OrderUrl);
            order.Payload = update.Payload;
            return order;
        }

        private async Task<OrderDetails?> CreateOrder(IEnumerable<string> identifiers, string cacheKey)
        {
            _log.Verbose("Creating certificate order for hosts: {identifiers}", identifiers);
            var order = await _client.CreateOrder(identifiers);
            // Check if the order is valid
            if ((order.Payload.Status != AcmeClient.OrderReady &&
                order.Payload.Status != AcmeClient.OrderPending) ||
                order.Payload.Error != null)
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

        /// <summary>
        /// Check if we have a recent order that can be reused
        /// to prevent hitting rate limits
        /// </summary>
        /// <param name="identifiers"></param>
        /// <returns></returns>
        private OrderDetails? FindRecentOrder(IEnumerable<string> identifiers, string cacheKey)
        {
            var fi = new FileInfo(Path.Combine(_orderPath.FullName, $"{cacheKey}.order.json"));
            if (!fi.Exists)
            {
                return null;
            }
            try
            {
                if (fi.LastWriteTime > DateTime.Now.AddDays(_settings.Cache.ReuseDays * -1))
                {
                    var content = File.ReadAllText(fi.FullName);
                    var order = JsonConvert.DeserializeObject<OrderDetails>(content);
                    if (order.Payload.Identifiers.Length == identifiers.Count())
                    {
                        if (order.Payload.Identifiers.All(x => 
                            string.Equals(x.Type, "dns", StringComparison.CurrentCultureIgnoreCase) && 
                            identifiers.Contains(x.Value.ToLower())))
                        {
                            return order;
                        }
                    }
                }
                else
                {
                    fi.Delete();
                }
            } 
            catch (Exception ex)
            {
                _log.Warning("Unable to read order cache: {ex}", ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Save order to disk
        /// </summary>
        /// <param name="order"></param>
        private void SaveOrder(OrderDetails order, string cacheKey)
        {
            //try
            //{
            //    if (!_orderPath.Exists)
            //    {
            //        _orderPath.Create();
            //    }
            //    var content = JsonConvert.SerializeObject(order);
            //    var path = Path.Combine(_orderPath.FullName, $"{cacheKey}.order.json");
            //    File.WriteAllText(path, content);
            //} 
            //catch (Exception ex)
            //{
            //    _log.Warning("Unable to save order: {ex}", ex.Message);
            //}
        }
    }
}
