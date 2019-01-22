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
            _input.Wait($"Create record {recordName} for domain {_identifier} with content {token} and press enter to continue...");
        }

        public override void DeleteRecord(string recordName, string token)
        {
            _input.Wait($"Delete record {recordName} for domain {_identifier}");
        }
    }
}
