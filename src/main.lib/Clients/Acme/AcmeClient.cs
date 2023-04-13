using ACMESharp;
using ACMESharp.Authorizations;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.X509;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients.Acme
{
    /// <summary>
    /// Main class that talks to the ACME server
    /// </summary>
    internal class AcmeClient 
    {
        /// <summary>
        /// https://tools.ietf.org/html/rfc8555#section-7.1.6
        /// </summary>
        public const string OrderPending = "pending"; // new order
        public const string OrderReady = "ready"; // all authorizations done
        public const string OrderProcessing = "processing"; // busy issuing
        public const string OrderInvalid = "invalid"; // validation/order error
        public const string OrderValid = "valid"; // certificate issued

        public const string AuthorizationValid = "valid";
        public const string AuthorizationInvalid = "invalid";
        public const string AuthorizationPending = "pending";
        public const string AuthorizationProcessing = "processing";

        public const string ChallengeValid = "valid";

        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly AcmeProtocolClient _client;

        /// <summary>
        /// Which account is this client authorized for
        /// </summary>
        public Account Account { get; private set; }

        public AcmeClient(
            ILogService log,
            ISettingsService settings,
            IProxyService proxy,
            ServiceDirectory directory,
            Account account)
        {
            _log = log;
            _settings = settings;
            var httpClient = proxy.GetHttpClient();
            httpClient.BaseAddress = settings.BaseUri;
            _client = new AcmeProtocolClient(httpClient, usePostAsGet: _settings.Acme.PostAsGet)
            {
                Directory = directory,
                Signer = account.Signer.JwsTool(),
                Account = account.Details
            };
            Account = account;
        }

        /// <summary>
        /// Create a new order
        /// </summary>
        /// <param name="identifiers"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal async Task<AcmeOrderDetails> CreateOrder(IEnumerable<Identifier> identifiers)
        {
            var acmeIdentifiers = identifiers.Select(i => new AcmeIdentifier()
            {
                Type = i.Type switch
                {
                    IdentifierType.DnsName => "dns", // rfc8555
                    IdentifierType.IpAddress => "ip", // rfc8738
                    _ => throw new NotImplementedException($"Identifier {i.Type} is not supported")
                },
                Value = i.Value
            });
            return await _client.Retry(() => _client.CreateOrderAsync(acmeIdentifiers), _log);
        }

        /// <summary>
        /// Get authorization details
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal async Task<AcmeAuthorization> GetAuthorizationDetails(string url) =>
            await _client.Retry(() => _client.GetAuthorizationDetailsAsync(url), _log);

        /// <summary>
        /// Get challenge details
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal async Task<AcmeChallenge> GetChallengeDetails(string url) =>
            await _client.Retry(() => _client.GetChallengeDetailsAsync(url), _log);

        /// <summary>
        /// Decode the challenge
        /// </summary>
        /// <param name="auth"></param>
        /// <param name="challenge"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        internal IChallengeValidationDetails DecodeChallengeValidation(AcmeAuthorization auth, AcmeChallenge challenge)
        {
            if (challenge.Type == null)
            {
                throw new NotSupportedException("Missing challenge type");
            }
            return AuthorizationDecoder.DecodeChallengeValidation(auth, challenge.Type, _client.Signer);
        }

        /// <summary>
        /// Answer the challenge
        /// </summary>
        /// <param name="challenge"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        internal async Task<AcmeChallenge> AnswerChallenge(AcmeChallenge challenge)
        {
            // Have to loop to wait for server to stop being pending
            if (challenge.Url == null)
            {
                throw new NotSupportedException("Missing challenge url");
            }
            challenge = await _client.Retry(() => _client.AnswerChallengeAsync(challenge.Url), _log);
            var tries = 1;
            while (
                challenge.Status == AuthorizationPending ||
                challenge.Status == AuthorizationProcessing)
            {
                if (challenge.Url == null)
                {
                    throw new NotSupportedException("Missing challenge url");
                }
                await Task.Delay(_settings.Acme.RetryInterval * 1000);
                _log.Debug("Refreshing authorization ({tries}/{count})", tries, _settings.Acme.RetryCount);
                challenge = await _client.Retry(() => _client.GetChallengeDetailsAsync(challenge.Url), _log);
                tries += 1;
                if (tries > _settings.Acme.RetryCount)
                {
                    break;
                }
            }
            return challenge;
        }

        /// <summary>
        /// Get order status
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal async Task<AcmeOrderDetails> GetOrderDetails(string url)
            => await _client.Retry(() => _client.GetOrderDetailsAsync(url), _log);

        /// <summary>
        /// Helper function to check/refresh order state
        /// </summary>
        /// <param name="details"></param>
        /// <param name="status"></param>
        /// <param name="negate"></param>
        /// <returns></returns>
        private async Task WaitForOrderStatus(AcmeOrderDetails details, string status, bool negate = false)
        {
            if (details.OrderUrl == null)
            {
                throw new InvalidOperationException();
            }

            var tries = 0;
            do
            {
                if (tries > 0)
                {
                    if (tries > _settings.Acme.RetryCount)
                    {
                        break;
                    }
                    _log.Debug($"Waiting for order to get {(negate ? "NOT " : "")}{{ready}} ({{tries}}/{{count}})", status, tries, _settings.Acme.RetryCount);
                    await Task.Delay(_settings.Acme.RetryInterval * 1000);
                    var update = await GetOrderDetails(details.OrderUrl);
                    details.Payload = update.Payload;
                }
                tries += 1;
            } while (
                (negate && details.Payload.Status == status) ||
                (!negate && details.Payload.Status != status)
            );
        }

        /// <summary>
        /// Deactive the authorization (in case of failure)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal async Task DeactivateAuthorization(string url) => 
            await _client.Retry(() => _client.DeactivateAuthorizationAsync(url), _log);

        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.3
        /// </summary>
        /// <param name="details"></param>
        /// <param name="csr"></param>
        /// <returns></returns>
        internal async Task<AcmeOrderDetails> SubmitCsr(AcmeOrderDetails details, byte[] csr)
        {
            // First wait for the order to get "ready", meaning that all validations
            // are complete. The program makes sure this is the case at the level of 
            // individual authorizations, but the server might need some extra time to
            // propagate this status at the order level.
            await WaitForOrderStatus(details, OrderReady);
            if (details.Payload.Status == OrderReady)
            {
                if (string.IsNullOrEmpty(details.Payload.Finalize))
                {
                    throw new Exception("Missing Finalize url");
                }
                details = await _client.Retry(() => _client.FinalizeOrderAsync(details, csr), _log);
                await WaitForOrderStatus(details, OrderProcessing, true);
            }
            return details;
        }

        /// <summary>
        /// Get certificate details
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        internal async Task<AcmeCertificate> GetCertificate(AcmeOrderDetails order) => 
            await _client.Retry(() => _client.GetOrderCertificateExAsync(order), _log);

        /// <summary>
        /// Download certificate bytes
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal async Task<byte[]> GetCertificate(string url)
        {
            return await _client.Retry(async () => {
                var response = await _client.GetAsync(url);
                return await response.Content.ReadAsByteArrayAsync();
            }, _log);
        }

        /// <summary>
        /// Revoke a certificate
        /// </summary>
        /// <param name="crt"></param>
        /// <returns></returns>
        internal async Task<bool> RevokeCertificate(byte[] crt) => 
            await _client.Retry(() => _client.RevokeCertificateAsync(crt, RevokeReason.Unspecified), _log);

        /// <summary>
        /// Get renewal info for a certificate
        /// </summary>
        /// <param name="crt"></param>
        /// <returns></returns>
        internal async Task<AcmeRenewalInfo?> GetRenewalInfo(ICertificateInfo certificate)
        {
            // Try to get renewalInfo from the server
            if (string.IsNullOrWhiteSpace(_client.Directory.RenewalInfo))
            {
                return null;
            }
            return await _client.Retry(() => _client.GetRenewalInfo(CertificateId(certificate)), _log);
        }

        /// <summary>
        /// Tell the server that we stopped caring about a certificate
        /// </summary>
        /// <param name="crt"></param>
        /// <returns></returns>
        internal async Task UpdateRenewalInfo(ICertificateInfo certificate)
        {
            if (string.IsNullOrWhiteSpace(_client.Directory.RenewalInfo))
            {
                return;
            }
            _ = await _client.Retry<object?>(async () => {
                await _client.UpdateRenewalInfo(CertificateId(certificate));
                return null; // we need to return something to make Retry<T>() happy
            }, _log);
        }

        private static byte[] CertificateId(ICertificateInfo certificate)
        {
            var bouncyCert = new X509CertificateParser().ReadCertificate(certificate.Certificate.GetRawCertData());
            var bouncyIssuer = new X509CertificateParser().ReadCertificate(certificate.Chain.First().GetRawCertData());
            var certificateId = new CertificateID(NistObjectIdentifiers.IdSha256.Id, bouncyIssuer, bouncyCert.SerialNumber);
            return certificateId.ToAsn1Object().GetDerEncoded();
        }

        /// <summary>
        /// Check account details
        /// </summary>
        /// <returns></returns>
        internal async Task<AccountDetails> CheckAccount() =>
            await _client.Retry(() => _client.CheckAccountAsync(), _log);

        /// <summary>
        /// Update contacts
        /// </summary>
        /// <param name="contacts"></param>
        /// <returns></returns>
        internal async Task<AccountDetails> UpdateAccountAsync(string[]? contacts) =>
            await _client.Retry(() => _client.UpdateAccountAsync(contacts), _log);
    }
}
