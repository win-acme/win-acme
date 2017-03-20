using ACMESharp;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple
{
    public class DnsForIISPlugin : Plugin
    {
        private DnsManagementClient _DnsClient;
        private Plugin _InnerPlugin = new IISPlugin();

        public override string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_DNS;

        public override string Name => "DNS";

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            if (_DnsClient != null)
                throw new InvalidOperationException($"Old authorization is still pending. Call {nameof(DeleteAuthorization)}() first!");

            var url = new UrlElements(answerPath);

            if (String.IsNullOrWhiteSpace(Program.Options.AzureTenantId))
                Program.Options.AzureTenantId = RequestViaCommandLine("Enter Azure Tenant ID: ");

            if (String.IsNullOrWhiteSpace(Program.Options.AzureClientId))
                Program.Options.AzureClientId = RequestViaCommandLine("Enter Azure Client ID: ");

            if (String.IsNullOrWhiteSpace(Program.Options.AzureSecret))
                Program.Options.AzureSecret = RequestViaCommandLine("Enter Azure Secret: ");

            if (String.IsNullOrWhiteSpace(Program.Options.AzureSubscriptionId))
                Program.Options.AzureSubscriptionId = RequestViaCommandLine("Enter Azure DNS Subscription ID: ");

            if (String.IsNullOrWhiteSpace(Program.Options.AzureResourceGroupName))
                Program.Options.AzureResourceGroupName = RequestViaCommandLine("Enter Azure DNS Resoure Group Name: ");

            // Build the service credentials and DNS management client
            var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(Program.Options.AzureTenantId, Program.Options.AzureClientId, Program.Options.AzureSecret).Result;
            _DnsClient = new DnsManagementClient(serviceCreds) { SubscriptionId = Program.Options.AzureSubscriptionId };

            // Create record set parameters
            var recordSetParams = new RecordSet();
            recordSetParams.TTL = 3600;

            // Add records to the record set parameter object.  In this case, we'll add a record of type 'TXT'
            recordSetParams.TxtRecords = new List<TxtRecord>();
            recordSetParams.TxtRecords.Add(new TxtRecord(new[] { fileContents }));

            // Create the actual record set in Azure DNS
            // Note: no ETAG checks specified, will overwrite existing record set if one exists
            var recordSet = _DnsClient.RecordSets.CreateOrUpdate(Program.Options.AzureResourceGroupName, url.Domain, url.Subdomain, RecordType.TXT, recordSetParams);
        }

        public override void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            if (_DnsClient != null)
            {
                var url = new UrlElements(answerPath);

                _DnsClient.RecordSets.Delete(Program.Options.AzureResourceGroupName, url.Domain, url.Subdomain, RecordType.TXT);
                _DnsClient.Dispose();
                _DnsClient = null;
            }
        }

        public override List<Target> GetSites()
        {
            return _InnerPlugin?.GetSites() ?? new List<Target>();
        }

        public override List<Target> GetTargets()
        {
            return _InnerPlugin?.GetTargets()
                                .Select(target => new Target
                                {
                                    PluginName = Name,
                                    AlternativeNames = target.AlternativeNames,
                                    Host = target.Host,
                                    SiteId = target.SiteId,
                                    WebRootPath = target.WebRootPath
                                })
                                .ToList()
                               ?? new List<Target>();
        }

        public override void Install(Target target)
        {
            _InnerPlugin?.Install(target);
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            _InnerPlugin?.Install(target, pfxFilename, store, certificate);
        }

        public override void Renew(Target target)
        {
            Auto(target);
        }

        private static string RequestViaCommandLine(string question)
        {
            var answer = String.Empty;

            for (int i = 0; i < 3 && String.IsNullOrWhiteSpace(answer); i++)
            {
                Console.Write(question);
                answer = Console.ReadLine();
            }

            return answer.Trim();
        }

        private class UrlElements
        {
            public UrlElements(String url)
            {
                var elements = url.Split('.');
                Domain = String.Join(".", elements.Skip(elements.Length - 2));
                Subdomain = String.Join(".", elements.Take(elements.Length - 2));
            }

            public string Domain { get; private set; }
            public string Subdomain { get; private set; }
        }
    }
}