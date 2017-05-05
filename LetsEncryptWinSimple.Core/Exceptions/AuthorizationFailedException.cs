using ACMESharp;
using System;

namespace LetsEncryptWinSimple.Core.Exceptions
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