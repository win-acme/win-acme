using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [IPlugin.Plugin<CentralSslOptions, CentralSslOptionsFactory, WacsJsonPlugins>
        ("af1f77b6-4e7b-4f96-bba5-c2eeb4d0dd42", CentralSslOptions.PluginName, "IIS Central Certificate Store (.pfx per host)")]
    internal class CentralSsl : IStorePlugin
    {
        private readonly ILogService _log;
        private readonly string _path;
        private readonly string? _password;

        public CentralSsl(
            ILogService log,
            ISettingsService settings,
            CentralSslOptions options,
            SecretServiceManager secretServiceManager)
        {
            _log = log;

            var passwordRaw = !string.IsNullOrWhiteSpace(options.PfxPassword?.Value) ?
                          options.PfxPassword.Value :
                          settings.Store.CentralSsl.DefaultPassword;
            _password = secretServiceManager.EvaluateSecret(passwordRaw);

            var path = !string.IsNullOrWhiteSpace(options.Path) ?
                options.Path :
                settings.Store.CentralSsl.DefaultPath;

            if (path != null && path.ValidPath(log))
            {
                _path = path;
                _log.Debug("Using CentralSsl path: {_path}", _path);
            }
            else
            {
                throw new Exception($"Specified CentralSsl path {path} is not valid.");
            }
        }

        private string PathForIdentifier(Identifier identifier) => Path.Combine(_path, $"{identifier.Value.Replace("*", "_")}.pfx");

        public async Task Save(CertificateInfo input)
        {
            _log.Information("Copying certificate to the CentralSsl store");
            foreach (var identifier in input.SanNames)
            {
                var dest = PathForIdentifier(identifier);
                _log.Information("Saving certificate to CentralSsl location {dest}", dest);
                try
                {
                    var collection = new X509Certificate2Collection
                    {
                        input.Certificate
                    };
                    collection.AddRange(input.Chain.ToArray());
                    await File.WriteAllBytesAsync(dest, collection.Export(X509ContentType.Pfx, _password)!);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error copying certificate to CentralSsl store");
                }
            }
            input.StoreInfo.TryAdd(
                GetType(),
                new StoreInfo()
                {
                    Name = CentralSslOptions.PluginName,
                    Path = _path
                });
        }

        public Task Delete(CertificateInfo input)
        {
            _log.Information("Removing certificate from the CentralSsl store");
            foreach (var identifier in input.SanNames)
            {
                var dest = PathForIdentifier(identifier);
                var fi = new FileInfo(dest);
                var cert = LoadCertificate(fi);
                if (cert != null)
                {
                    if (string.Equals(cert.Thumbprint, input.Certificate.Thumbprint, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _log.Warning("Delete {fi} with thumb {thumb}", fi.FullName, cert.Thumbprint);
                        fi.Delete();
                    }
                    cert.Dispose();
                }               
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Load certificate from disk
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        private X509Certificate2? LoadCertificate(FileInfo fi)
        {
            X509Certificate2? cert = null;
            if (!fi.Exists)
            {
                return cert;
            }
            try
            {
                cert = new X509Certificate2(fi.FullName, _password);
            }
            catch (CryptographicException)
            {
                try
                {
                    cert = new X509Certificate2(fi.FullName, "");
                }
                catch
                {
                    _log.Warning("Unable to scan certificate {name}", fi.FullName);
                }
            }
            return cert;
        }
    }
}
