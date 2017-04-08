using ACMESharp;
using System;

namespace LetsEncrypt.ACME.Simple
{
    public class AuthorizationFailedException : Exception
    {
        public AuthorizationState authorizationState { get; set; }

        public AuthorizationFailedException(AuthorizationState state)
        {
            authorizationState = state;
        }
    }
}
