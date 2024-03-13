using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ZoneStatus>))]
internal enum ZoneStatus
{
    Verified,
    Failed,
    Pending
}