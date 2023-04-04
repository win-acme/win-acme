using System.Collections.Generic;
using Newtonsoft.Json;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Models;

internal class DomainListResponse
{
    [JsonProperty("data")]
    public ICollection<DomainListResponseDomain> Data { get; set; }
}