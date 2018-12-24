using System.IO;

namespace PKISharp.WACS.Services.Renewal.Legacy
{
    internal class FileLegacyRenewalService : BaseLegacyRenewalService
    {
        private const string _renewalsKey = "Renewals";

        public FileLegacyRenewalService(
            ILogService log,
            IOptionsService options,
            SettingsService settings) : base(settings, options, log)
        { }

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

        }
    }
}
