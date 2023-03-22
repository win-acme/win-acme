using Autofac;
using Autofac.Core;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS
{
    internal class RenewalRevoker
    {
        private readonly ISharingLifetimeScope _container;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly AcmeClientManager _clientManager;

        public RenewalRevoker(
            ISharingLifetimeScope container,
            IAutofacBuilder autofacBuilder, 
            ExceptionHandler exceptionHandler,
            AcmeClientManager clientManager)
        {
            _container = container;
            _scopeBuilder = autofacBuilder;
            _exceptionHandler = exceptionHandler;
            _clientManager = clientManager;
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
                var client = await _clientManager.GetClient(renewal.Account);
                using var scope = _scopeBuilder.Execution(_container, renewal, client, RunLevel.Unattended); ;
                var cs = scope.Resolve<ICertificateService>();
                try
                {
                    await cs.RevokeCertificate(renewal);
                    renewal.History.Add(new RenewResult("Certificate(s) revoked"));
                }
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex);
                }
            }
        }
    }
}