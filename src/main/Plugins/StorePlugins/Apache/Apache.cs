using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class Apache : IStorePlugin
    {
        private ILogService _log;
        private readonly string _path;

        public Apache(ILogService log, ApacheOptions options)
        {
            _log = log;
            if (!string.IsNullOrWhiteSpace(options.Path))
            {
                _path = options.Path;
            }
            if (_path.ValidPath(log))
            {
                _log.Debug("Using Apache certificate path: {_path}", _path);
            }
            else
            {
                throw new Exception("Error initializing Apache plugin, specified path is not valid.");
            }
        }

        public void Save(CertificateInfo input)
        {
            _log.Information("Copying certificate to the Apache folder");
            // TODO: generate chain.pem and key.pem from the .pfx, so that they
            // don't have to be generate and stored in the certificate service anymore
            try
            {
                var name = input.PfxFile.Name.Replace("-all.pfx", "");
                var chain = input.PfxFile.Directory.GetFiles($"*{name}-chain.pem").FirstOrDefault();
                if (chain != null)
                {
                    chain.CopyTo(Path.Combine(_path, $"{input.SubjectName}-chain.pem"), true);
                }
                else
                {
                    throw new Exception("Missing certificate chain file");
                }

                var key = input.PfxFile.Directory.GetFiles($"*{name}-key.pem").FirstOrDefault();
                if (chain != null)
                {
                    key.CopyTo(Path.Combine(_path, $"{input.SubjectName}-key.pem"), true);
                }
                else
                {
                    throw new Exception("Missing certificate key file");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error copying files to Apache folder");
            }
        }

        public void Delete(CertificateInfo input)
        {
            // Not supported
        }

        public CertificateInfo FindByThumbprint(string thumbprint)
        {
            return null;
        }
    }
}
