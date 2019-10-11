using ACMESharp.Authorizations;
using Org.BouncyCastle.Asn1;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Tls
{
    internal class SelfHosting : Validation<TlsAlpn01ChallengeValidationDetails>
    {
        internal const int DefaultValidationPort = 443;
        private TcpListener _listener;
        private X509Certificate2 _certificate;
        private readonly string _identifier;
        private readonly SelfHostingOptions _options;
        private readonly ILogService _log;
        private readonly UserRoleService _userRoleService;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();


        public SelfHosting(ILogService log, string identifier, SelfHostingOptions options, UserRoleService userRoleService)
        {
            _identifier = identifier;
            _log = log;
            _options = options;
            _userRoleService = userRoleService;
        }

        public async Task RecieveRequests()
        {
            while (true)
            {
                using var client = await _listener.AcceptTcpClientAsync();
                using var sslStream = new SslStream(client.GetStream());
                var sslOptions = new SslServerAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol>
                    {
                        new SslApplicationProtocol("acme-tls/1")
                    },
                    ServerCertificate = _certificate
                };
                await sslStream.AuthenticateAsServerAsync(sslOptions, _tokenSource.Token);
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        public override Task CleanUp()
        {
            try
            {
                _tokenSource.Cancel();
                _listener.Stop();
            } 
            catch 
            { 
            }
            return Task.CompletedTask;
        }

        public override Task PrepareChallenge()
        {
            try
            {
                using var rsa = RSA.Create(2048);
                var name = new X500DistinguishedName($"CN={_identifier}");

                var request = new CertificateRequest(
                    name,
                    rsa, 
                    HashAlgorithmName.SHA256, 
                    RSASignaturePadding.Pkcs1);

                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(_challenge.TokenValue));
                request.CertificateExtensions.Add(
                    new X509Extension(
                        new AsnEncodedData("1.3.6.1.5.5.7.1.31", 
                            new DerOctetString(hash).GetDerEncoded()), 
                            true));

                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName(_identifier);
                request.CertificateExtensions.Add(sanBuilder.Build());

                _certificate = request.CreateSelfSigned(
                    new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), 
                    new DateTimeOffset(DateTime.UtcNow.AddDays(1)));

                _certificate = new X509Certificate2(
                    _certificate.Export(X509ContentType.Pfx, _identifier),
                    _identifier,
                    X509KeyStorageFlags.MachineKeySet);

                _listener = new TcpListener(IPAddress.Any, _options.Port ?? DefaultValidationPort);
                _listener.Start();

                Task.Run(RecieveRequests);
            }
            catch
            {
                _log.Error("Unable to activate TcpClient, this may be because of insufficient rights or another application using port 443");
                throw;
            }
            return Task.CompletedTask;
        }

        public override bool Disabled => IsDisabled(_userRoleService);
        internal static bool IsDisabled(UserRoleService userRoleService) => !userRoleService.IsAdmin;
    }
}
