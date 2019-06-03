using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using DNS.Server.Acme;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class SelfDNS : DnsValidation<SelfDNSOptions, SelfDNS>
    {
        private DnsServerAcme selfDnsServer;
        public SelfDNS(
            LookupClientProvider dnsClient,
            ILogService log,
            SelfDNSOptions options, string
            identifier) :
            base(dnsClient, log, options, identifier)
        {
        }
        public override void PrepareChallenge()
        {
            //setup for temporary DNS Server
            selfDnsServer = new DnsServerAcme(_log);

            CreateRecord(_challenge.DnsRecordName, _challenge.DnsRecordValue);
            selfDnsServer.Listen();

            PreValidate(true);
        }
        public override void CreateRecord(string recordName, string token)
        {
           selfDnsServer.AddRecord(recordName, token);
            _log.Information("Validation TXT {token} added to DNS Server {answerUri}", token, recordName);
        }
        public override void DeleteRecord(string recordName, string token)
        {          
        }
        public override void CleanUp()
        {
            selfDnsServer.Dispose();
            base.CleanUp();
        }
    }
}
