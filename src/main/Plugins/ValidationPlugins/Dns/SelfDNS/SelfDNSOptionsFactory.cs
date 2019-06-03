using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Clients.DNS;
using DNS.Server.Acme;
using System.Net;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class SelfDNSOptionsFactory : ValidationPluginOptionsFactory<SelfDNS, SelfDNSOptions>
    {
        private readonly LookupClientProvider _dnsClient;
        private readonly IInputService _input;

        public SelfDNSOptionsFactory(ILogService log, LookupClientProvider dnsClient,
            IInputService input) : base(log, Constants.Dns01ChallengeType)
        {
            _dnsClient = dnsClient;
            _input = input;
        }

        public override SelfDNSOptions Aquire(Target target, IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            const string testTXT = "custom TXTrecord";

            //find external IP address
            IPAddress serverIP = IPAddress.Parse("8.8.8.8");
            string externalip = "";
            try
            {
                externalip = new WebClient().DownloadString("http://icanhazip.com").Replace("\n", "");
                serverIP = IPAddress.Parse(externalip);
            }
            catch
            {
                _log.Error("couldn't get server's external IP address");
            }

            _log.Information("To set up self-hosted DNS validation, ensure the following three steps:");
            _log.Information(" 1. You have opened port 53 in your firewall for incoming requests");
            _log.Information(" 2. You have created a DNS A record that points to this server ({IP})", externalip);
            _log.Information(" 3. You have created the following records with your domain host's DNS manager:");

            var identifiers = target.Parts.SelectMany(x => x.Identifiers);
            identifiers = identifiers.Select(x => x.Replace("*.", "").Insert(0,"_acme-challenge.")).Distinct();
            foreach (var identifier in identifiers)
            {
                _log.Information("   NS for {identifier} ", identifier);
            }
            _log.Information("Note: Each NS record should point to this server's name (from step 2)");

            using (DnsServerAcme server = new DnsServerAcme(_log))
            {
                foreach (var identifier in identifiers)
                {
                    //create a test DNS record for each identifier
                    server.AddRecord(identifier, testTXT);
                }

                //start by pre-checking to see whether lookup works by starting a server
                //and then doing a lookup for a test record or just seeing whether the event fires.
                server.Listen();
                bool retry = false;
                do
                {
                    try
                    {
                        //use the test server as the name server to check the first identifier
                        //this should work even if the NS record is not yet set up in the DNS zone
                        _log.Information("Checking that port 53 is open on {IP}...", externalip);
                        var TXTResponse = _dnsClient.GetClient(serverIP).GetTextRecordValues(identifiers.First()).ToList();
                        if (TXTResponse.Any() && TXTResponse.First() == testTXT)
                        {
                            _log.Information("Port 53 is open and the DNS server is operating correctly");
                            break;
                        }
                    }
                    catch
                    {
                        _log.Error("An error occurred checking port 53");
                    }
                    retry = _input.PromptYesNo("The DNS server is not exposed on port 53. Would you like to try again?", false);
                } while (retry);

                do
                {
                    try
                    {
                        int failCount = identifiers.Count();
                        foreach (var identifier in identifiers)
                        {
                            server.reqReceived = false;
                            _log.Information("Checking NS record setup for {identifier}", identifier);
                            var TXTResponse = _dnsClient.DefaultClient.GetTextRecordValues(identifier);
                            if (TXTResponse.Contains(testTXT))
                            {
                                _log.Information("Successful lookup for {identifier}", identifier);
                                failCount -= 1;
                            }
                            else if (TXTResponse.Any())
                            {
                                _log.Warning("TXT record found for {identifier} but it appears to be coming from a different DNS server. Check the NS record", identifier);
                            }
                            else if (server.reqReceived)
                            {
                                _log.Warning("A DNS request was received but may have the wrong domain name. Check your NS record");
                            }
                            else
                            {
                                _log.Warning("No request was received by the DNS server");
                            }
                        }
                        if (failCount == 0)
                        {
                            _log.Information("Your NS records are working correctly");
                            break;
                        }
                    }
                    catch
                    {
                        _log.Error("An error occurred while checking records");
                    }
                    retry = _input.PromptYesNo("Some of your NS entries are not working. Would you like to test the DNS entries again?", false);
                } while (retry);
            }
            return new SelfDNSOptions();            
        }
        public override SelfDNSOptions Default(Target target, IArgumentsService arguments)
        {
            return new SelfDNSOptions();
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
