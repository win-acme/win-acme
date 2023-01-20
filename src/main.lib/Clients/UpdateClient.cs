using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    class UpdateClient
    {
        private readonly ILogService _log;
        private readonly IProxyService _proxy;
        private readonly WacsJson _wacsJson;

        public UpdateClient(ILogService log, IProxyService proxy, WacsJson wacsJson)
        {
            _log = log;
            _proxy = proxy;
            _wacsJson = wacsJson;   
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
                var data = JsonSerializer.Deserialize(json, WacsJson.Insensitive.VersionCheckData);
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

        internal class VersionCheckData 
        {
            public VersionData? Latest { get; set; }
        }

        internal class VersionData
        {
            public string? Name { get; set; }
            public string? Tag { get; set; }
            public string? Build { get; set; }
        }
    }
}
