using System;
using System.Diagnostics;

namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Information about when the certificate is due
    /// </summary>
    [DebuggerDisplay("{Start}-{End}")]
    public class DueDate : IComparable<DueDate>
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Source { get; set; }

        public int CompareTo(DueDate? other)
        {
            if (other == null)
            {
                return -1;
            }
            return End.CompareTo(other.End);
        }
    }
}
