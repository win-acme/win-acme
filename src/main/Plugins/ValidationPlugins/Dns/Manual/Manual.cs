using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class Manual : DnsValidation<ManualOptions, Manual>
    {
        private IInputService _input;

        public Manual(
            LookupClientProvider dnsClient,  
            ILogService log, 
            IInputService input, 
            ManualOptions options, string 
            identifier) : 
            base(dnsClient, log, options, identifier)
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

            // Verify
            var client = _dnsClientProvider.GetClient(_identifier);
            while (true)
            {
                if (client.GetTextRecordValues(recordName).Any(x => x == token))
                {
                    break;
                }
                else
                {
                    var retry = _input.PromptYesNo(
                        "The correct record is not yet found by the local resolver. " +
                        "Check your configuration and/or wait for the name servers to " +
                        "synchronize and press <Enter> to try again. Answer 'N' to " +
                        "try ACME validation anyway.", true);
                    if (!retry)
                    {
                        break;
                    }
                }
            }
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
