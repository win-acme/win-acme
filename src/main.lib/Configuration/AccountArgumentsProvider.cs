using Fclp;

namespace PKISharp.WACS.Configuration
{
    internal class AccountArgumentsProvider : BaseArgumentsProvider<AccountArguments>
    {
        public override string Name => "Account";
        public override string Group => "";
        public override string Condition => "";

        protected override bool IsActive(AccountArguments current)
        {
            return
                current.AcceptTos ||
                !string.IsNullOrEmpty(current.EabAlgorithm) ||
                !string.IsNullOrEmpty(current.EabKey) ||
                !string.IsNullOrEmpty(current.EabKeyIdentifier) ||
                !string.IsNullOrEmpty(current.EmailAddress);
        }

        public override void Configure(FluentCommandLineParser<AccountArguments> parser)
        {
            // Acme account registration
            parser.Setup(o => o.AcceptTos)
                .As("accepttos")
                .WithDescription("Accept the ACME terms of service.");

            parser.Setup(o => o.EmailAddress)
                .As("emailaddress")
                .WithDescription("Email address to use by ACME for renewal fail notices.");

            // External account binding
            parser.Setup(o => o.EabKeyIdentifier)
                .As("eab-key-identifier")
                .WithDescription("Key identifier to use for external account binding.");

            parser.Setup(o => o.EabKey)
                .As("eab-key")
                .WithDescription("Key to use for external account binding. Must be base64url encoded.");

            parser.Setup(o => o.EabAlgorithm)
              .As("eab-algorithm")
              .WithDescription("Algorithm to use for external account binding. Valid values are HS256 (default), HS384, and HS512.");
        }
    }
}