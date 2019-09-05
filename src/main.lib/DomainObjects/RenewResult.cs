using PKISharp.WACS.Extensions;
using System;

namespace PKISharp.WACS.DomainObjects
{
    public class RenewResult
    {
        public DateTime Date { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Thumbprint { get; set; }

        private RenewResult()
        {
            Date = DateTime.UtcNow;
        }

        public RenewResult(CertificateInfo certificate) : this()
        {
            Success = true;
            Thumbprint = certificate.Certificate.Thumbprint;
        }

        public RenewResult(string error) : this()
        {
            Success = false;
            ErrorMessage = error;
        }

        public override string ToString() => $"{Date} " +
            $"- {(Success ? "Success" : "Error")}" +
            $"{(string.IsNullOrEmpty(Thumbprint) ? "" : $" - Thumbprint {Thumbprint}")}" +
            $"{(string.IsNullOrEmpty(ErrorMessage) ? "" : $" - {ErrorMessage.ReplaceNewLines()}")}";
    }
}
