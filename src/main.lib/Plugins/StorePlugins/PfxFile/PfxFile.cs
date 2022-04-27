using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using bc = Org.BouncyCastle;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PfxFile : IStorePlugin
    {
        private readonly ILogService _log;
        private readonly string _path;
        private readonly string? _password;

        public static string? DefaultPath(ISettingsService settings) => 
            settings.Store.PfxFile?.DefaultPath;

        public static string? DefaultPassword(ISettingsService settings)
            => settings.Store.PfxFile?.DefaultPassword;

        public PfxFile(
            ILogService log, 
            ISettingsService settings, 
            PfxFileOptions options,
            SecretServiceManager secretServiceManager)
        {
            _log = log;

            var passwordRaw = !string.IsNullOrWhiteSpace(options.PfxPassword?.Value) ?
                options.PfxPassword.Value :
                settings.Store.PfxFile?.DefaultPassword;
            _password = secretServiceManager.EvaluateSecret(passwordRaw);

            var path = !string.IsNullOrWhiteSpace(options.Path) ? 
                options.Path :
                settings.Store.PfxFile?.DefaultPath;

            if (path != null && path.ValidPath(log))
            {
                _path = path;
                _log.Debug("Using pfx file path: {_path}", _path);
            }
            else
            {
                throw new Exception($"Specified pfx file path {path} is not valid.");
            }
        }

        private string PathForIdentifier(string identifier) => Path.Combine(_path, $"{identifier.Replace("*", "_")}.pfx");

        public async Task Save(CertificateInfo input)
        {
            var dest = PathForIdentifier(input.CommonName.Value);
            _log.Information("Copying certificate to the pfx folder {dest}", dest);
            try
            {
                var collection = new X509Certificate2Collection
                {
                    input.Certificate
                };
                collection.AddRange(input.Chain.ToArray());
                var ms = new MemoryStream(collection.Export(X509ContentType.Pfx)!);
                var bcPfx = new bc.Pkcs.Pkcs12Store(ms, Array.Empty<char>());
                var aliases = bcPfx.Aliases.OfType<string>().ToList();
                var key = default(bc.Pkcs.AsymmetricKeyEntry);
                var chain = default(bc.Pkcs.X509CertificateEntry[]);
                foreach (var alias in aliases)
                {
                    if (bcPfx.IsKeyEntry(alias))
                    {
                        key = bcPfx.GetKey(alias);
                        chain = bcPfx.GetCertificateChain(alias);
                        break;
                    }
                }
                using var fs = new FileInfo(dest).OpenWrite();
                bcPfx = new bc.Pkcs.Pkcs12Store();
                bcPfx.SetKeyEntry(input.CommonName.Value, key, chain);
                bcPfx.Save(fs, _password?.ToCharArray(), new bc.Security.SecureRandom());
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error copying certificate to pfx path");
            }
            input.StoreInfo.TryAdd(
                GetType(),
                new StoreInfo()
                {
                    Name = PfxFileOptions.PluginName,
                    Path = _path
                });
        }

        public Task Delete(CertificateInfo input) => Task.CompletedTask;

        (bool, string?) IPlugin.Disabled => (false, null);
    }
}
