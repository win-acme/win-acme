using Autofac;
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

        private IISClient _iisClient;
        private IOptionsService _optionsService;

        public override string Description => "Use IIS as endpoint";
        public override string Name => "IIS";
        public override IValidationPlugin CreateInstance(Target target) => new IIS(target);
        public override void Aquire(IOptionsService options, IInputService input, Target target) { }
        public override void Default(IOptionsService options, Target target) { }
     
        public IIS()
        {
            _iisClient = new IISClient();
            _optionsService = Program.Container.Resolve<IOptionsService>();
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
            return IISClient.Version.Major >= 8;
        }

        public override void InstallCertificate(Target target, CertificateInfo certificate)
        {
            Program.SaveCertificate(certificate);
            AddToIIS(certificate);
        }

        public override void RemoveCertificate(Target target, CertificateInfo certificate)
        {
            Program.DeleteCertificate(certificate);
            RemoveFromIIS(certificate);
        }

        private void AddToIIS(CertificateInfo certificate)
        {
            var host = certificate.HostNames.First();
            Site site;
            if (_tempSiteId == null)
            {
                site = _iisClient.ServerManager.Sites.Add(host, "http", string.Format("*:80:{0}", host), "X:\\");
                _tempSiteId = site.Id;
            }
            else
            {
                site = _iisClient.ServerManager.Sites.Where(x => x.Id == _tempSiteId).FirstOrDefault();
            }

            SSLFlags flags = SSLFlags.SNI;
            if (_optionsService.Options.CentralSsl)
            {
                flags |= SSLFlags.CentralSSL;
            }
            _iisClient.AddOrUpdateBindings(site, host, flags, certificate.Certificate.GetCertHash(), certificate.Store?.Name);
            _iisClient.Commit();
        }

        private void RemoveFromIIS(CertificateInfo certificate)
        {
            var host = certificate.HostNames.First();
            if (_tempSiteId != null)
            {
                var site = _iisClient.ServerManager.Sites.Where(x => x.Id == _tempSiteId).FirstOrDefault();
                if (_tempSiteCreated)
                {
                    _iisClient.ServerManager.Sites.Remove(site);
                }
                else
                {
                    var binding = site.Bindings.Where(x => string.Equals(x.Host, host, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    site.Bindings.Remove(binding);
                }
                _iisClient.Commit();
            }
        }
    }
}
