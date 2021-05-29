using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TransIp.Library.Dto
{
    [DebuggerDisplay("{Name}")]
    public class Domain
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
            
        [JsonProperty("authCode")]
        public string? AuthorizationCode { get; set; }
            
        [JsonProperty("isTransferLocked")]
        public bool TransferLocked { get; set; }
            
        [JsonProperty("registrationDate")]
        public DateTime RegistrationDate { get; set; }

        [JsonProperty("renewalDate")]
        public DateTime RenewalDate { get; set; }     
        
        [JsonProperty("isWhitelabel")]
        public bool Whitelabel { get; set; }

        [JsonProperty("cancellationDate")]
        public DateTime? CancellationDate { get; set; } 
        
        [JsonProperty("cancellationStatus")]
        public string? CancellationStatus { get; set; }
        
        [JsonProperty("isDnsOnly")]
        public bool DnsOnly { get; set; } 
        
        [JsonProperty("tags")]
        public IEnumerable<string>? Tags { get; set; }     
    }

    public class DomainList
    {
        [JsonProperty("domains")]
        public IEnumerable<Domain>? Domains { get; set; } 
    }
}
