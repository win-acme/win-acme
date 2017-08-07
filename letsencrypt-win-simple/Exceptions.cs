using ACMESharp;
using System;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple
{
    public class AuthorizationFailedException : Exception
    {
        public AuthorizationState authorizationState { get; private set; }
        public IEnumerable<string> acmeErrorMessages { get; private set; }

        public AuthorizationFailedException(AuthorizationState state, IEnumerable<string> acmeErrors)
        {
            authorizationState = state;
            acmeErrorMessages = acmeErrors;
        }
    }
}
