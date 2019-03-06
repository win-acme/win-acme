using Nager.PublicSuffix;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Interfaces;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class Manual : DnsValidation<ManualOptions, Manual>
    {
        private IInputService _input;

        public Manual(DomainParser domainParser, ILookupClientProvider lookupClientProvider, ILogService log, IInputService input, ManualOptions options, string identifier) :  base(domainParser, lookupClientProvider, log, options, identifier)
        {
            // Usually it's a big no-no to rely on user input in validation plugin
            // because this should be able to run unattended. This plugin is for testing
            // only and therefor we will allow it. Future versions might be more advanced,
            // e.g. shoot an email to an admin and complete the order later.
            _input = input;
        }
        
        public override void CreateRecord(string recordName, string token)
        {
            _input.Show("Domain", _identifier, true);
            _input.Show("Record", recordName);
            _input.Show("Type", "TXT");
            _input.Show("Content", $"\"{token}\"");
            _input.Show("Note 1", "Some DNS control panels add quotes automatically. Only one set is required.");
            _input.Show("Note 2", "Make sure your name servers are synchronised, this may take several minutes!");
            _input.Wait("Please press enter after you've created and verified the record");
        }

        public override void DeleteRecord(string recordName, string token)
        {
            _input.Show("Domain", _identifier, true);
            _input.Show("Record", recordName);
            _input.Show("Type", "TXT");
            _input.Show("Content", $"\"{token}\"");
            _input.Wait("Please press enter after you've deleted the record");
        }
    }
}
