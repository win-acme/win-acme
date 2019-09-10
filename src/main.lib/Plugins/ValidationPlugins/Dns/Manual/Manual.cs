using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Manual : DnsValidation<Manual>
    {
        private readonly IInputService _input;
        private readonly string _identifier;

        public Manual(
            LookupClientProvider dnsClient, ILogService log, 
            IInputService input, string identifier) : base(dnsClient, log)
        {
            // Usually it's a big no-no to rely on user input in validation plugin
            // because this should be able to run unattended. This plugin is for testing
            // only and therefor we will allow it. Future versions might be more advanced,
            // e.g. shoot an email to an admin and complete the order later.
            _input = input;
            _identifier = identifier;
        }

        public override async Task CreateRecord(string recordName, string token)
        {
            _input.Show("Domain", _identifier, true);
            _input.Show("Record", recordName);
            _input.Show("Type", "TXT");
            _input.Show("Content", $"\"{token}\"");
            _input.Show("Note", "Some DNS managers add quotes automatically. A single set is needed.");
            await _input.Wait("Please press enter after you've created and verified the record");

            // Pre-pre-validate, allowing the manual user to correct mistakes
            while (true)
            {
                if (await PreValidate(0))
                {
                    break;
                }
                else
                {
                    var retry = await _input.PromptYesNo(
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

        public override Task DeleteRecord(string recordName, string token)
        {
            _input.Show("Domain", _identifier, true);
            _input.Show("Record", recordName);
            _input.Show("Type", "TXT");
            _input.Show("Content", $"\"{token}\"");
            _input.Wait("Please press enter after you've deleted the record");
            return Task.CompletedTask;
        }
    }
}
