using System;
using DNS.Client;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Client.RequestResolver;

namespace DNS.Server.Acme
{
    class DnsServerAcme : IDisposable
    {
        private AcmeTXTRecs DnsRecords;
        private DnsServer selfDnsServer;
        private ILogService _log;
        public bool reqReceived = false;
        public DnsServerAcme(ILogService log)
        {
            _log = log;
            //initialization here for DNS Server events and record storage
            DnsRecords = new AcmeTXTRecs();
            selfDnsServer = new DnsServer(DnsRecords);
            selfDnsServer.Responded += (sender, e) =>
            {
                reqReceived = true;
                _log.Information("DNS Server received lookup request from {remote}", e.Remote.Address.ToString());
                var questions = e.Request.Questions;
                foreach (var question in questions)
                {
                    _log.Debug("DNS Request: " + question.ToString());
                }
                var answers = e.Response.AnswerRecords;
                foreach (var answer in answers)
                {
                    _log.Debug("DNS Response: " + answer.ToString());
                }
            };
            selfDnsServer.Listening += (sender, e) => _log.Information("DNS Server listening...");
            selfDnsServer.Errored += (sender, e) =>
            {
                _log.Debug("Errored: {Error}", e.Exception);
                ResponseException responseError = e.Exception as ResponseException;
                if (responseError != null) _log.Debug(responseError.Response.ToString());
            };
        }
        //Addrecord adds a TXT record to the DNS Server
        public void AddRecord(string recordName, string token)
        {
            DnsRecords.AddTextResourceRecord(recordName, token);
        }
        public async void Listen()
        {
            await selfDnsServer.Listen();
        }

        public void Dispose()
        {
            selfDnsServer.Dispose();
        }


        //AcmeTXTRecs implements IRequestResolver, the database of DNS records used by DnsServer.
        //The standard implementation had two problems for letsencrypt: it didn't handle the mixed-
        //case requests that letsencrypt performs. And it assumed an attribute value name that prevented matching.
        //this version only supports TXT records.
        private class AcmeTXTRecs : IRequestResolver
        {
            private static readonly TimeSpan DEFAULT_TTL = new TimeSpan(0);

            private static bool Matches(Domain domain, Domain entry)
            {
                string[] labels = entry.ToString().Split('.');
                string[] patterns = new string[labels.Length];

                for (int i = 0; i < labels.Length; i++)
                {
                    string label = labels[i];
                    patterns[i] = label == "*" ? "(\\w+)" : Regex.Escape(label);
                }

                Regex re = new Regex("^" + string.Join("\\.", patterns) + "$", RegexOptions.IgnoreCase);
                return re.IsMatch(domain.ToString());
            }

            private static void Merge<T>(IList<T> l1, IList<T> l2)
            {
                foreach (T obj in l2)
                {
                    l1.Add(obj);
                }
            }

            private IList<IResourceRecord> entries = new List<IResourceRecord>();
            private TimeSpan ttl = DEFAULT_TTL;

            public void AddTextResourceRecord(string domain, string token)
            {
                entries.Add(new TextResourceRecord(new Domain(domain), CharacterString.FromString($"{token}"), ttl));
            }

            public Task<IResponse> Resolve(IRequest request)
            {
                IResponse response = Response.FromRequest(request);

                foreach (Question question in request.Questions)
                {
                    IList<IResourceRecord> answers = Get(question.Name, question.Type);

                    if (answers.Count > 0)
                    {
                        Merge(response.AnswerRecords, answers);
                    }
                    else
                    {
                        response.ResponseCode = ResponseCode.NameError;
                    }
                }
                return Task.FromResult(response);
            }

            private IList<IResourceRecord> Get(Domain domain, RecordType type)
            {
                return entries.Where(e => Matches(domain, e.Name) && e.Type == type).ToList();
            }
        }
    }
}
