using System;
using System.Collections.Generic;

namespace PKISharp.WACS.DomainObjects
{
    public class OrderResult
    {
        public DateTime? ExpireDate { get; set; }

        public string Name { get; private set; }

        public bool? Success { get; set; }

        public string? Thumbprint { get; set; }

        public OrderResult AddErrorMessage(string? value, bool fatal = true)
        {

            if (value != null)
            {
                if (ErrorMessages == null)
                {
                    ErrorMessages = new List<string>();
                }
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

        public OrderResult(string name) => Name = name;

        public OrderResult(string name, string error) : this(name)
        {
            Success = false;
            AddErrorMessage(error);
        }
    }
}
