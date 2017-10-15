using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using static LetsEncrypt.ACME.Simple.Clients.IISClient;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Tls
{
    class IIS : TlsValidation
    {
        private long? _tempSiteId;
        private bool _tempSiteCreated = false;
        private string _storeName = Properties.Settings.Default.CertificateStore;

        private IISClient _iisClient;
        public override string Description => "Use IIS as TLS endpoint";
        public override string Name => "IIS";
        public override IValidationPlugin CreateInstance(Target target) => new IIS(target);
        public override void Aquire(Options options, InputService input, Target target) { }
        public override void Default(Options options, Target target) { }
     
        public IIS()
        {
            _iisClient = new IISClient();
        }
   
        public IIS(Target target) : this()
        {
            if (target.IIS == true)
            {
                _tempSiteId = target.SiteId;
            }
        }

        public override bool CanValidate(Target target)
        {
            return false; // Pending AcmeSharp... _iisClient.Version.Major >= 8;
        }

        public override void InstallCertificate(Target target, X509Certificate2 certificate, string host)
        {
            Program.SaveCertificate(new List<string> { host }, certificate);
            AddToIIS(host, certificate.GetCertHash());
        }

        public override void RemoveCertificate(Target target, X509Certificate2 certificate, string host)
        {
            Program.DeleteCertificate(certificate.Thumbprint);
            RemoveFromIIS(host);
        }

        private void AddToIIS(string host, byte[] certificateHash)
        {
            Site site;
            if (_tempSiteId == null)
            {
                site = _iisClient.GetServerManager().Sites.Add(host, "http", string.Format("*:80:{0}", host), "X:\\");
                _tempSiteId = site.Id;
            }
            else
            {
                site = _iisClient.GetServerManager().Sites.Where(x => x.Id == _tempSiteId).FirstOrDefault();
            }

            SSLFlags flags = SSLFlags.SNI;
            if (Program.Options.CentralSsl)
            {
                flags |= SSLFlags.CentralSSL;
            }
            _iisClient.AddOrUpdateBindings(site, host, flags, certificateHash, _storeName);
            _iisClient.GetServerManager().CommitChanges();
        }

        private void RemoveFromIIS(string host)
        {
            if (_tempSiteId != null)
            {
                var site = _iisClient.GetServerManager().Sites.Where(x => x.Id == _tempSiteId).FirstOrDefault();
                if (_tempSiteCreated)
                {
                    _iisClient.GetServerManager().Sites.Remove(site);
                }
                else
                {
                    var binding = site.Bindings.Where(x => string.Equals(x.Host, host, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    site.Bindings.Remove(binding);
                }
                _iisClient.GetServerManager().CommitChanges();
            }
        }
    }
}
