using PKISharp.WACS.Extensions;
using System;

namespace PKISharp.WACS
{
    public class RenewResult
    {
        public DateTime Date { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Thumbprint { get; set; }

        private RenewResult()
        {
            Date = DateTime.Now;
        }

        public RenewResult(CertificateInfo certificate) : this()
        {
            Success = true;
            Thumbprint = certificate.Certificate.Thumbprint;
        }

        public RenewResult(Exception ex) : this()
        {
            Success = false;
            ErrorMessage = ex.Message;
        }

        public override string ToString() => $"{Date.ToUserString()} " +
            $"- {(Success ? "Success" : "Error")}" +
            $"{(string.IsNullOrEmpty(Thumbprint) ? "" : $" - Thumbprint {Thumbprint}")}" +
            $"{(string.IsNullOrEmpty(ErrorMessage) ? "" : $" - {ErrorMessage.ReplaceNewLines()}")}";
    }
}
