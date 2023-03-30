using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.DomainObjects
{
    [DebuggerDisplay("{Name}")]
    public class OrderResult
    {
        public DateTime? ExpireDate { get; set; }

        public DueDate? DueDate { get; set; }

        public string Name { get; private set; }

        public bool? Success { get; set; }

        public bool? Missing { get; set; }

        public string? Thumbprint { get; set; }

        public OrderResult AddErrorMessage(string? value, bool fatal = true)
        {

            if (value != null)
            {
                ErrorMessages ??= new List<string>();
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

        public List<string>? ErrorMessages { get; set; }

        [JsonConstructor]
        public OrderResult(string name) => Name = name;
    }
}
