using System;
using ACMESharp;
using ACMESharp.PKI;
using LetsEncrypt.ACME.Simple.Core.Configuration;

namespace LetsEncrypt.ACME.Simple.Core.Interfaces
{
    public interface ILetsEncryptService
    {
        AuthorizationState Authorize(Target target);
        void WarmupSite(Uri uri);
        string GetCertificate(Target binding);
        string GetIssuerCertificate(CertificateRequest certificate, CertificateProvider cp);
    }
}