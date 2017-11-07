using ACMESharp;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    public interface IValidationPluginFactory : IHasName
    {
        /// <summary>
        /// Type of challange
        /// </summary>
        string ChallengeType { get; }

        /// <summary>
        /// Is this plugin capable of validating the target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanValidate(Target target);

        /// <summary>
        /// Which type is used as instance
        /// </summary>
        Type Instance { get; }
    }

    class NullValidationFactory : IValidationPluginFactory, IIsNull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IValidationPluginFactory.Instance => typeof(object);
        string IValidationPluginFactory.ChallengeType => string.Empty;
        bool IValidationPluginFactory.CanValidate(Target target) => false;
    }

    public interface IValidationPlugin
    {
        /// <summary>
        /// Check or get information need for validation (interactive)
        /// </summary>
        /// <param name="target"></param>
        void Aquire(Target target);

        /// <summary>
        /// Check information need for validation (unattended)
        /// </summary>
        /// <param name="target"></param>
        void Default(Target target);

        /// <summary>
        /// Prepare challenge
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="challenge"></param>
        /// <returns></returns>
        Action<AuthorizationState> PrepareChallenge(ScheduledRenewal renewal, AuthorizeChallenge challenge, string identifier);
    }
}
