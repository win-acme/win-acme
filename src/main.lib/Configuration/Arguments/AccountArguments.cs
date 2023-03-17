namespace PKISharp.WACS.Configuration.Arguments
{
    public class AccountArguments : BaseArguments
    {
        public override string Name => "Account";

        [CommandLine(Description = "Accept the ACME terms of service.")]
        public bool AcceptTos { get; set; }

        [CommandLine(Description = "Email address to link to your ACME account.")]
        public string? EmailAddress { get; set; }

        [CommandLine(Description = "Name for your ACME account. A seperate registration and signer will be generated for each unique value that you provide.")]
        public string? Account { get; set; }

        [CommandLine(Name = "eab-key-identifier", Description = "Key identifier to use for external account binding.")]
        public string? EabKeyIdentifier { get; set; }

        [CommandLine(Name = "eab-key", Description = "Key to use for external account binding. Must be base64url encoded.", Secret = true)]
        public string? EabKey { get; set; }

        [CommandLine(Name = "eab-algorithm", Description = "Algorithm to use for external account binding. Valid values are HS256 (default), HS384, and HS512.")]
        public string? EabAlgorithm { get; set; }
    }
}