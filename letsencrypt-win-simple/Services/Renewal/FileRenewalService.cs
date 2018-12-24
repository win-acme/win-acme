using System.IO;

namespace PKISharp.WACS.Services.Renewal
{
    internal class FileRenewalService : BaseRenewalService
    {
        private const string _renewalsKey = "Renewals_v2";

        public FileRenewalService(
            ILogService log,
            IOptionsService options,
            ISettingsService settings) : base(settings, options, log)
        {
            _log.Verbose("Store renewals in file {FileName}", FileName);
        }

        private string FileName => Path.Combine(_configPath, _renewalsKey);

        internal override string[] RenewalsRaw
        {
            get
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
            set
            {
                File.WriteAllLines(FileName, value);
            }
        }
    }
}
