using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class Script : ScriptClient, IInstallationPlugin
    {
        private Renewal _renewal;
        private ScriptOptions _options;

        public Script(Renewal renewal, ScriptOptions options, ILogService logService) : base(logService)
        {
            _options = options;
            _renewal = renewal;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            RunScript(
                  _options.Script,
                  _options.ScriptParameters,
                  newCertificate.SubjectName, // {0}
                  _renewal.PfxPassword, // {1}
                  newCertificate.PfxFile.FullName, // {2}
                  newCertificate.Store?.Name ?? "[None]", // {3}
                  newCertificate.Certificate.FriendlyName, // {4}
                  newCertificate.Certificate.Thumbprint, // {5}
                  newCertificate.PfxFile.Directory.FullName, // {6}
                  _renewal.Id); // {7}
        }
    }
}