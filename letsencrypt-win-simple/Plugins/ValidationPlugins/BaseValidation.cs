using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for all validation plugins
    /// </summary>
    abstract class BaseValidation<T> : IValidationPlugin where T : Challenge
    {
        protected ILogService _log;
        protected string _identifier;
        protected T _challenge;

        public BaseValidation(ILogService logService, string identifier)
        {
            _log = logService;
            _identifier = identifier;
        }

        /// <summary>
        /// Must implement
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Handle the challenge
        /// </summary>
        /// <param name="challenge"></param>
        public void PrepareChallenge(AuthorizeChallenge challenge)
        {
            if (challenge.Challenge.GetType() != typeof(T))
            {
                throw new InvalidOperationException();
            }
            else
            {
                _challenge = (T)challenge.Challenge;
                PrepareChallenge();
            }
        }

        /// <summary>
        /// Handle the challenge
        /// </summary>
        /// <param name="challenge"></param>
        public abstract void PrepareChallenge();
    }
}
