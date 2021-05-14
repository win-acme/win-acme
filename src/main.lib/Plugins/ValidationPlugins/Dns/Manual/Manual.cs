using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Manual : DnsValidation<Manual>
    {
        private readonly IInputService _input;

        public override ParallelOperations Parallelism => ParallelOperations.Answer;

        public Manual(
            LookupClientProvider dnsClient, 
            ILogService log, 
            IInputService input,
            ISettingsService settings) : 
            base(dnsClient, log, settings)
            // Usually it's a big no-no to rely on user input in validation plugin
            // because this should be able to run unattended. This plugin is for testing
            // only and therefor we will allow it. Future versions might be more advanced,
            // e.g. shoot an email to an admin and complete the order later.
            => _input = input;

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            _input.CreateSpace();
            _input.Show("Domain", record.Context.Identifier);
            _input.Show("Record", record.Authority.Domain);
            _input.Show("Type", "TXT");
            _input.Show("Content", $"\"{record.Value}\"");
            _input.Show("Note", "Some DNS managers add quotes automatically. A single set is needed.");
            if (!await _input.Wait("Please press <Enter> after you've created and verified the record"))
            {
                _log.Warning("User aborted");
                return false;
            }

            if (!_settings.Validation.PreValidateDns)
            {
                return true;
            }

            // Pre-pre-validate, allowing the manual user to correct mistakes
            while (true)
            {
                if (await PreValidate(record))
                {
                    return true;
                }
                else
                {
                    _input.CreateSpace();
                    _input.Show(null, value: "The correct record has not yet been found by the local resolver. That means it's likely the validation attempt will fail, or your DNS provider needs a little more time to publish and synchronize the changes.");
                    var options = new List<Choice<bool?>>
                    {
                        Choice.Create<bool?>(null, "Retry check"),
                        Choice.Create<bool?>(true, "Ignore and continue"),                        
                        Choice.Create<bool?>(false, "Abort")
                    };
                    var chosen = await _input.ChooseFromMenu("How would you like to proceed?", options);
                    if (chosen != null)
                    {
                        return chosen.Value;
                    }
                }
            }
        }

        public override Task DeleteRecord(DnsValidationRecord record)
        {
            _input.CreateSpace();
            _input.Show("Domain", record.Context.Identifier);
            _input.Show("Record", record.Authority.Domain);
            _input.Show("Type", "TXT");
            _input.Show("Content", $"\"{record.Value}\"");
            _input.Wait("Please press <Enter> after you've deleted the record");
            return Task.CompletedTask;
        }
    }
}
