using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LetsEncrypt.ACME.Simple.Services;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Web.Administration;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Tls
{
    class IIS : TlsValidation
    {
        public override string Description => "Use IIS default website as TLS endpoint";
        public override string Name => nameof(IIS);

        public override void Aquire(Options options, InputService input, Target target) {}

        public override void Default(Options options, Target target)
        {
            throw new NotImplementedException();
        }

        public override void InstallCertificate(Target target, string identifier, X509Certificate2 certificate)
        {
            var storeName = "WebHosting";
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();

            using (var iisManager = new ServerManager())
            {
                var site = iisManager.Sites.Add("Temp", "http", "*:80:zzz.temp", "X:\\");
                var binding = site.Bindings.Add("*:443:zzz.temp", certificate.GetCertHash(), storeName);
                binding.SetAttributeValue("sslFlags", 1); // Enable SNI support
                iisManager.CommitChanges();
            }
        }

        public override void RemoveCertificate(Target target, string identifier, X509Certificate2 certificate)
        {
            X509Store store = new X509Store("Let's Encrypt validation", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            X509Certificate2Collection col = store.Certificates;
            foreach (var cert in col)
                if (cert.Thumbprint == certificate.Thumbprint)
                    store.Remove(cert);
            store.Close();
        }
    }
}
