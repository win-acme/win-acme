using ACMESharp.Authorizations;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHosting : Validation<Http01ChallengeValidationDetails>
    {
        internal const int DefaultValidationPort = 80;
        private HttpListener _listener;
        private readonly Dictionary<string, string> _files;
        private readonly SelfHostingOptions _options;
        private readonly ILogService _log;
        private readonly UserRoleService _userRoleService;

        public SelfHosting(ILogService log, SelfHostingOptions options, UserRoleService userRoleService)
        {
            _log = log;
            _options = options;
            _files = new Dictionary<string, string>();
            _userRoleService = userRoleService;
        }

        public async Task ReceiveRequests()
        {
            while (_listener.IsListening)
            {
                var ctx = await _listener.GetContextAsync();
                var path = ctx.Request.Url.LocalPath;
                if (_files.TryGetValue(path, out var response))
                {
                    _log.Verbose("SelfHosting plugin serving file {name}", path);
                    using var writer = new StreamWriter(ctx.Response.OutputStream);
                    writer.Write(response);
                }
                else
                {
                    _log.Warning("SelfHosting plugin couldn't serve file {name}", path);
                    ctx.Response.StatusCode = 404;
                }
            }
        }

        public override Task CleanUp()
        {
            try
            {
                _listener.Stop();
                _listener.Close();
            } catch {
            }
            _listener = null;
            return Task.CompletedTask;
        }

        public override Task PrepareChallenge()
        {
            _files.Add("/" + _challenge.HttpResourcePath, _challenge.HttpResourceValue);
            try
            {
                var prefix = $"http://+:{_options.Port ?? DefaultValidationPort}/.well-known/acme-challenge/";
                _listener = new HttpListener();
                _listener.Prefixes.Add(prefix);
                _listener.Start();
                Task.Run(ReceiveRequests);
            }
            catch
            {
                _log.Error("Unable to activate HttpListener, this may be because of insufficient rights or a non-Microsoft webserver using port 80");
                throw;
            }
            return Task.CompletedTask;
        }

        public override bool Disabled => IsDisabled(_userRoleService);
        internal static bool IsDisabled(UserRoleService userRoleService) => !userRoleService.IsAdmin;
    }
}
