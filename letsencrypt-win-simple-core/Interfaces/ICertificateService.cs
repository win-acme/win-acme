using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using ACMESharp.JOSE;
using LetsEncrypt.ACME.Simple.Core.Configuration;
using LetsEncrypt.ACME.Simple.Core.Schedules;

namespace LetsEncrypt.ACME.Simple.Core.Interfaces
{
    public interface ICertificateService
    {
        void InstallCertificate(Target binding, string pfxFilename, out X509Store store,
            out X509Certificate2 certificate);

        void UninstallCertificate(string host, out X509Store store, X509Certificate2 certificate);
        void GetCertificateForTargetId(List<Target> targets, int targetId);
        void GetCertificatesForAllHosts(List<Target> targets);
        void LoadSignerFromFile(RS256Signer signer, string signerPath);
        void CheckRenewalsAndWaitForEnterKey();
        void CheckRenewals();
        void ProcessRenewal(List<ScheduledRenewal> renewals, DateTime now, ScheduledRenewal renewal);
        void ProcessDefaultCommand(List<Target> targets, string command);
    }
}