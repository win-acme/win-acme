using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Services
{
    internal class CertificatePicker 
    {
        private readonly ISettingsService _settings;
        private readonly ILogService _log;

        public CertificatePicker(
            ILogService log,
            AcmeClientManager client,
            PemService pemService,
            ISettingsService settingsService)
        {
            _log = log;
            _settings = settingsService;
        }

        /// <summary>
        /// Get the name for the root issuer
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        private static string? Root(CertificateOption option) => 
            option.WithoutPrivateKey.Chain.LastOrDefault()?.IssuerClean() ?? 
            option.WithoutPrivateKey.Certificate.IssuerClean();

        /// <summary>
        /// Choose between different versions of the certificate
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public CertificateOption Select(List<CertificateOption> options)
        {
            var selected = options[0];
            if (options.Count > 1)
            {
                _log.Debug("Found {n} version(s) of the certificate", options.Count);
                foreach (var option in options)
                {
                    _log.Debug("Option {n} issued by {issuer} (thumb: {thumb})", 
                        options.IndexOf(option) + 1, 
                        Root(option), 
                        option.WithPrivateKey.Certificate.Thumbprint);
                }
                if (!string.IsNullOrEmpty(_settings.Acme.PreferredIssuer))
                {
                    var match = options.FirstOrDefault(x => string.Equals(Root(x), _settings.Acme.PreferredIssuer, StringComparison.InvariantCultureIgnoreCase));
                    if (match != null)
                    {
                        selected = match;
                    }
                }
                _log.Debug("Selected option {n}", options.IndexOf(selected) + 1);
            }
            if (!string.IsNullOrEmpty(_settings.Acme.PreferredIssuer) &&
                !string.Equals(Root(selected), _settings.Acme.PreferredIssuer, StringComparison.InvariantCultureIgnoreCase))
            {
                _log.Warning("Unable to find certificate issued by preferred issuer {issuer}", _settings.Acme.PreferredIssuer);
            }
            return selected;
        }
    }
}