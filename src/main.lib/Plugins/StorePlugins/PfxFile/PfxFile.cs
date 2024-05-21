using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [IPlugin.Plugin<
        PfxFileOptions, PfxFileOptionsFactory, 
        DefaultCapability, WacsJsonPlugins>
        ("2a2c576f-7637-4ade-b8db-e8613b0bb33e",
        Name, "PFX archive")]
    internal class PfxFile : IStorePlugin
    {
        internal const string Name = "PfxFile";

        private readonly ILogService _log;
        private readonly string _path;
        private readonly string? _name;
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

            var passwordRaw = 
                options.PfxPassword?.Value ?? 
                settings.Store.PfxFile?.DefaultPassword;
            _password = secretServiceManager.EvaluateSecret(passwordRaw);
            _name = options.FileName;

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

        public async Task<StoreInfo?> Save(ICertificateInfo input)
        {
            try
            {
                // Change PK alias to something more predictable
                var outputBuilder = new Pkcs12StoreBuilder();
                var output = outputBuilder.Build();
                var aliases = input.Collection.Aliases.ToList();
                var keyAlias = aliases.FirstOrDefault(a => input.Collection.IsKeyEntry(a));
                if (keyAlias != null)
                {
                    output.SetKeyEntry(
                        input.CommonName?.Value ?? input.SanNames.First().Value,
                        input.Collection.GetKey(keyAlias),
                        input.Collection.GetCertificateChain(keyAlias));
                }
                else
                {
                    foreach (var alias in aliases)
                    {
                        output.SetCertificateEntry(alias, input.Collection.GetCertificate(alias));
                    }
                }

                var dest = PathForIdentifier(_name ?? input.CommonName?.Value ?? input.SanNames.First().Value);
                var outInfo = new CertificateInfo(output);
                _log.Information("Copying certificate to the pfx folder {dest}", dest);
                await outInfo.PfxSave(dest, _password);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error copying certificate to pfx path");
            }
            return new StoreInfo() {
                Name = Name,
                Path = _path
            };
        }

        public Task Delete(ICertificateInfo input) => Task.CompletedTask;
    }
}
