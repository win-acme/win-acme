using Microsoft.Win32;
using System.IO;

namespace LetsEncrypt.ACME.Simple.Services.Renewal
{
    class FileRenewalService : BaseRenewalService
    {
        private const string _renewalsKey = "Renewals";
        private string _path;

        public FileRenewalService(
            ILogService log,
            IOptionsService options,
            SettingsService settings) : base(settings, options, log)
        {
            _log.Verbose("Store renewals in file {FileName}", FileName);
        }

        private string FileName
        {
            get
            {
                return Path.Combine(_configPath, _renewalsKey);
            }
        }

        internal override string[] ReadRenewalsRaw()
        {
            if (File.Exists(FileName))
            {
                return File.ReadAllLines(FileName);
            }
            else
            {
                return null;
            }
        }

        internal override void WriteRenewalsRaw(string[] Renewals)
        {
            File.WriteAllLines(FileName, Renewals);
        }
    }
}
