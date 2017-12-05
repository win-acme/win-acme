using ACMESharp;
using System;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple
{
    public class AuthorizationFailedException : Exception
    {
        public AuthorizationState AuthorizationState { get; private set; }
        public IEnumerable<string> AcmeErrorMessages { get; private set; }

        public AuthorizationFailedException(AuthorizationState state, IEnumerable<string> acmeErrors)
        {
            AuthorizationState = state;
            AcmeErrorMessages = acmeErrors;
        }
    }
}
