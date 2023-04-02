using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS
{
    internal class RenewalRevoker
    {
        private readonly ExceptionHandler _exceptionHandler;
        private readonly AcmeClientManager _clientManager;
        private readonly IRenewalStore _renewalStore;
        private readonly ICacheService _cacheService;
        private readonly OrderManager _orderManager;
        private readonly DueDateStaticService _dueDate;
        private readonly ILogService _log;

        public RenewalRevoker(
            ExceptionHandler exceptionHandler,
            ICacheService cacheService,
            IRenewalStore renewalStore,
            ILogService log,
            OrderManager orderManager,
            AcmeClientManager clientManager,
            DueDateStaticService dueDate)
        {
            _exceptionHandler = exceptionHandler;
            _cacheService = cacheService;
            _clientManager = clientManager;
            _orderManager = orderManager;
            _renewalStore = renewalStore;
            _dueDate = dueDate;
            _log = log;
        }

        /// <summary>
        /// Shared code for command line and renewal manager
        /// </summary>
        /// <param name="renewals"></param>
        /// <returns></returns>
        internal async Task RevokeCertificates(IEnumerable<Renewal> renewals)
        {
            foreach (var renewal in renewals)
            {
                try
                {
                    _log.Warning($"Revoke renewal {renewal.LastFriendlyName}");
                    var client = await _clientManager.GetClient(renewal.Account);
                    var orders = _dueDate.CurrentOrders(renewal);
                    var result = new RenewResult()
                    {
                        OrderResults = new List<OrderResult>()
                    };
                    foreach (var order in orders.Where(x => !x.Revoked))
                    {
                        var cache = _cacheService.PreviousInfo(renewal, order.Key);
                        if (cache != null)
                        {
                            try
                            {
                                var certificateDer = cache.Certificate.Export(X509ContentType.Cert);
                                await client.RevokeCertificate(certificateDer);
                                result.OrderResults.Add(new OrderResult(order.Key) { Revoked = true });
                            }
                            catch (Exception ex)
                            {
                                result.OrderResults.Add(new OrderResult(order.Key)
                                {
                                    ErrorMessages = new List<string>() { $"Error revoking ({ex.Message})" }
                                });
                                _log.Warning("Error revoking for {order}: {ex}", order, ex.Message);
                            }
                        }
                        else
                        {
                            _log.Debug("No certificate found for {order}", order.Key);
                            result.OrderResults.Add(new OrderResult(order.Key)
                            {
                                ErrorMessages = new List<string>() { $"Error revoking (cert not found)" }
                            });
                        }
                    }

                    // Make sure private keys are not reused after this
                    _cacheService.Revoke(renewal);
                    _renewalStore.Save(renewal, result);
                }
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex);
                }
            }

            // Delete order cache to prevent any chance of the
            // revoked certificates being reused on the a run
            _orderManager.ClearCache();
        }

        /// <summary>
        /// Shared code for command line and renewal manager
        /// </summary>
        /// <param name="renewals"></param>
        /// <returns></returns>
        internal async Task CancelRenewals(IEnumerable<Renewal> renewals)
        {
            foreach (var renewal in renewals)
            {
                _log.Warning($"Cancelling renewal {renewal.LastFriendlyName}");
                var client = await _clientManager.GetClient(renewal.Account);
                var orders = _dueDate.CurrentOrders(renewal);
                foreach (var order in orders)
                {
                    var cache = _cacheService.PreviousInfo(renewal, order.Key);
                    if (cache != null)
                    {
                        try
                        {
                            await client.UpdateRenewalInfo(cache);
                        }
                        catch (Exception ex)
                        {
                            _log.Warning($"Error updating renewalInfo for {order}: {ex}", ex.Message);
                        }
                    }
                    else
                    {
                        _log.Debug($"No certificate found for {order}");
                    }
                }
                try
                {
                    _renewalStore.Cancel(renewal);
                    _cacheService.Delete(renewal);
                }
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex);
                }
            }
        }
    }
}