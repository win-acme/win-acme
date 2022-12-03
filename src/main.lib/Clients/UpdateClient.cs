using PKISharp.WACS.Services;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    class UpdateClient
    {
        private readonly ILogService _log;
        private readonly IProxyService _proxy;

        public UpdateClient(ILogService log, IProxyService proxy)
        {
            _log = log;
            _proxy = proxy;
        }

        public async Task CheckNewVersion()
        {
            try
            {
                var httpClient = _proxy.GetHttpClient();
                var json = await httpClient.GetStringAsync("https://www.win-acme.com/version.json");
                if (string.IsNullOrEmpty(json))
                {
                    throw new Exception("Empty result");
                }
                var data = JsonSerializer.Deserialize<VersionCheckData>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                if (data == null || data.Latest == null || data.Latest.Build == null)
                {
                    throw new Exception("Invalid result");
                }
                var latestVersion = new Version(data.Latest.Build);
                if (latestVersion > VersionService.SoftwareVersion)
                {
                    var updateInstruction = VersionService.DotNetTool ?
                        "Use \"dotnet tool update win-acme\" to update." : 
                        "Download from https://www.win-acme.com/";
                    _log.Warning($"New version {{latestVersion}} available! {updateInstruction}", latestVersion);
                }
                else
                {
                    _log.Information($"You are running the latest version of the program");
                }
            } 
            catch (Exception ex)
            {
                _log.Error(ex, "Version check failed");
            }
        }

        private class VersionCheckData 
        {
            public VersionData? Latest { get; set; }
        }

        private class VersionData
        {
            public string? Name { get; set; }
            public string? Tag { get; set; }
            public string? Build { get; set; }
        }
    }
}
