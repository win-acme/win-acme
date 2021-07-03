using ACMESharp.Authorizations;
using PKISharp.WACS.Context;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal sealed class Rest : Validation<Http01ChallengeValidationDetails>
    {
        private readonly ConcurrentBag<(string url, string challengeValue)> _urlsChallenges = new();
        private readonly IProxyService _proxyService;
        private readonly ILogService _log;
        private readonly string? _securityToken;
        private readonly bool _useHttps;

        public override ParallelOperations Parallelism => ParallelOperations.Prepare | ParallelOperations.Answer;

        public Rest(
            IProxyService proxyService,
            ILogService log,
            SecretServiceManager ssm,
            RestOptions options)
        {
            _proxyService = proxyService;
            _log = log;
            _securityToken = ssm.EvaluateSecret(options.SecurityToken);
            _useHttps = options.UseHttps == true;
        }

        public override Task PrepareChallenge(ValidationContext context, Http01ChallengeValidationDetails challenge)
        {
            var resourceUrl = challenge.HttpResourceUrl;
            if (_useHttps)
            {
                resourceUrl = resourceUrl.Replace("http://", "https://");
            }
            _urlsChallenges.Add((resourceUrl, challenge.HttpResourceValue));
            return Task.CompletedTask;
        }

        public override async Task Commit()
        {
            _log.Information("Sending verification files to the server(s)");

            using var client = GetClient();

            var responses = await Task.WhenAll(_urlsChallenges
                .Select(item => client.PutAsync(item.url, new StringContent(item.challengeValue))));

            var isError = false;
            foreach (var resp in responses.Where(r => !r.IsSuccessStatusCode))
            {
                isError = true;
                _log.Error("Error {ErrorCode} sending verification file to server {Server}", resp.StatusCode, resp.RequestMessage?.RequestUri?.Host);
            }
            if (isError)
            {
                throw new Exception("Failure sending verification files to one or more servers");
            }
        }

        public override async Task CleanUp()
        {
            _log.Information("Removing verification files from the server(s)");

            using var client = GetClient();

            var responses = await Task.WhenAll(_urlsChallenges
                .Select(item => client.DeleteAsync(item.url)));
            
            foreach (var resp in responses.Where(r => !r.IsSuccessStatusCode))
            {
                _log.Warning("Error {ErrorCode} removing verification file from server {Server}", resp.StatusCode, resp.RequestMessage?.RequestUri?.Host);
            }
        }

        private HttpClient GetClient()
        {
            var client = _proxyService.GetHttpClient(false);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _securityToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
