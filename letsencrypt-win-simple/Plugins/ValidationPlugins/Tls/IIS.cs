using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

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

        public IIS() { }
        public IIS(Target target)
        {
            _iisClient = (IISClient)Program.Plugins.GetByName(Program.Plugins.Legacy, IISClient.PluginName);
            if (target.IIS == true)
            {
                _tempSiteId = target.SiteId;
            }
        }

        public override void InstallCertificate(Target target, X509Certificate2 certificate, string host)
        {
            AddToStore(certificate);
            AddToIIS(host, certificate.GetCertHash());
        }

        public override void RemoveCertificate(Target target, X509Certificate2 certificate, string host)
        {
            RemoveFromStore(certificate);
            RemoveFromIIS(host);
        }

        private void AddToStore(X509Certificate2 certificate)
        {
            X509Store store = new X509Store(_storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();
        }

        private void RemoveFromStore(X509Certificate2 certificate)
        {
            X509Store store = new X509Store(_storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            foreach (var cert in store.Certificates)
                if (cert.Thumbprint == certificate.Thumbprint)
                    store.Remove(cert);
            store.Close();
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
            _iisClient.AddOrUpdateBinding(site, host, IISClient.SSLFlags.SNI, certificateHash, _storeName);
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
