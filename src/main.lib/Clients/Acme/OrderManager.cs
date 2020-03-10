using ACMESharp.Protocol;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients.Acme
{
    class OrderManager
    {
        private readonly ILogService _log;
        private readonly AcmeClient _client;

        public OrderManager(ILogService log, AcmeClient client)
        {
            _log = log;
            _client = client;
        }

        public async Task<OrderDetails?> GetOrCreate(IEnumerable<string> identifiers)
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
            }
            return order;
        }
    }
}
