using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.DomainObjects
{
    public class RenewResult
    {
        /// <summary>
        /// Date the renewal was run
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// (Minimum) expire date for the set of certificates 
        /// that was ordered during this renewal.
        /// </summary>
        public DateTime? ExpireDate
        {
            get
            {
                if (_expireDate != null && _expireDate.HasValue)
                {
                    return _expireDate.Value;
                }
                return OrderResults?.Select(x => x.ExpireDate).Min();
            }
            [Obsolete("Only for legacy")]
            set => _expireDate = value;
        }
        private DateTime? _expireDate = null;

        /// <summary>
        /// Was the renewal aborted by the user
        /// </summary>
        [JsonIgnore]
        public bool Abort { get; set; }

        /// <summary>
        /// Was the renewal succesfully completed
        /// </summary>
        public bool? Success { get; set; }

        [JsonPropertyName("OrderResults")]
        public List<OrderResult>? OrderResultsJson { get; set; }

        /// <summary>
        /// Results for individual orders generated from
        /// the renewal that was run
        /// </summary>
        [JsonIgnore]
        public List<OrderResult> OrderResults
        {
            get
            {
                var ret = OrderResultsJson;
                if (ret == null)
                {
                    ret = new List<OrderResult>();
                    if (ThumbprintsJson != null && ThumbprintsJson.Any())
                    {
                        foreach (var thumb in ThumbprintsJson)
                        {
                            ret.Add(new OrderResult(!ret.Any() ? "main" : "legacy")
                            {
                                Thumbprint = thumb,
                                ExpireDate = _expireDate,
                                Success = Success
                            });
                        }
                    }
                    else
                    {
                        ret.Add(new OrderResult("main")
                        {
                            Success = Success,
                            ExpireDate = _expireDate,
                        });
                    }
                }
                return ret;
            }
            set => OrderResultsJson = value;
        }

        [JsonPropertyName("Thumbprints")]
        public List<string>? ThumbprintsJson { get; set; }

        [JsonIgnore]
        public List<string> Thumbprints
        {
            get
            {
                if (OrderResultsJson?.Any() ?? false)
                {
                    return OrderResultsJson.
                        Where(x => !string.IsNullOrEmpty(x.Thumbprint)).
                        Select(x => x.Thumbprint).
                        OfType<string>().
                        ToList();
                }
                if (ThumbprintsJson != null)
                {
                    return ThumbprintsJson;
                }
                return new List<string>();
            }
        }

        [JsonIgnore]
        public string ThumbprintSummary => string.Join("|", Thumbprints.OrderBy(x => x));

        /// <summary>
        /// All general error messages
        /// </summary>
        public List<string>? ErrorMessages { get; set; }

        public RenewResult() => Date = DateTime.UtcNow;

        /// <summary>
        /// Return immediate error result
        /// </summary>
        /// <param name="error"></param>
        public RenewResult(string error) : this()
        {
            Success = false;
            ErrorMessages = new List<string> { error };
        }

        public override string ToString() => $"{Date} " +
            $"- {(Success == true ? "Success" : "Error")}" +
            $"{((OrderResults?.Count ?? 0) == 0 ? "" : $" - Orders {string.Join(", ", OrderResults!.Select(x => x.Name))}")}" +
            $"{((ErrorMessages?.Count ?? 0) == 0 ? "" : $" - {string.Join(", ", ErrorMessages!.Select(x => x.ReplaceNewLines()))}")}";
    }
}
