using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.DomainObjects
{
    public class RenewResult
    {
        public DateTime Date { get; set; }
        public DateTime? ExpireDate { get; set; }
        [JsonIgnore]
        public bool Abort { get; set; }

        public bool Success { get; set; }

        public string? ErrorMessage { set => AddErrorMessage(value); }

        public string? Thumbprint { set => AddThumbprint(value); }

        public RenewResult AddErrorMessage(string? value, bool fatal = true)
        {
            if (value != null)
            {
                if (!ErrorMessages.Contains(value))
                {
                    ErrorMessages.Add(value);
                }
            }
            if (fatal)
            {
                Success = false;
            }
            return this;
        }

        public void AddThumbprint(string? value)
        {
            if (value != null)
            {
                if (!Thumbprints.Contains(value))
                {
                    Thumbprints.Add(value);
                }
            }
        }

        public List<string> Thumbprints { get; set; } = new List<string>();
        public string ThumbprintSummary => string.Join("|", Thumbprints.OrderBy(x => x));
        public List<string> ErrorMessages { get; set; } = new List<string>();

        public RenewResult() 
        {
            Success = true;
            Date = DateTime.UtcNow;
        }

        public RenewResult(string error) : this()
        {
            Success = false;
            AddErrorMessage(error);
        }

        public override string ToString() => $"{Date} " +
            $"- {(Success ? "Success" : "Error")}" +
            $"{(Thumbprints.Count == 0 ? "" : $" - Thumbprint {string.Join(", ", Thumbprints)}")}" +
            $"{(ErrorMessages.Count == 0 ? "" : $" - {string.Join(", ", ErrorMessages.Select(x => x.ReplaceNewLines()))}")}";
    }
}
