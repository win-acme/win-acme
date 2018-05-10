using ACMESharp;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS
{
    public class AuthorizationFailedException : Exception
    {
        public AuthorizationState AuthorizationState { get; }
        public IEnumerable<string> AcmeErrorMessages { get; }

        public AuthorizationFailedException(AuthorizationState state, IEnumerable<string> acmeErrors)
        {
            AuthorizationState = state;
            AcmeErrorMessages = acmeErrors;
        }
    }
}
