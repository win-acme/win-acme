using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class Manual : DnsValidation<ManualOptions, Manual>
    {
        private IInputService _input;

        public Manual(ILogService log, IInputService input, ManualOptions options, string identifier) :  base(log, options, identifier)
        {
            // Usually it's a big no-no to rely on user input in validation plugin
            // because this should be able to run unattended. This plugin is for testing
            // only and therefor we will allow it. Future versions might be more advanced,
            // e.g. shoot an email to an admin and complete the order later.
            _input = input;
        }
        
        public override void CreateRecord(string recordName, string token)
        {
            _log.Warning("Create record {recordName} for domain {identifier} with content {token}", recordName, _identifier, token);
            _input.Wait();
        }

        public override void DeleteRecord(string recordName)
        {
            _log.Warning("Delete record {recordName} for domain {identifier}", recordName, _identifier);
            _input.Wait();
        }
    }
}
