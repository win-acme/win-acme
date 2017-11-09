using ACMESharp;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    public interface IValidationPluginFactory : IHasName, IHasType
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
    }

    abstract class BaseValidationPluginFactory<T> : BasePluginFactory<T>, IValidationPluginFactory where T : IValidationPlugin
    {
        private string _challengeType;
        public BaseValidationPluginFactory(string name, string description, string challengeType) : base(name, description)
        {
            _challengeType = challengeType;
        }
        string IValidationPluginFactory.ChallengeType => _challengeType;
        public virtual bool CanValidate(Target target) { return true; }
    }

    /// <summary>
    /// Null implementation
    /// </summary>
    class NullValidationFactory : IValidationPluginFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IHasType.Instance => typeof(object);
        string IValidationPluginFactory.ChallengeType => string.Empty;
        bool IValidationPluginFactory.CanValidate(Target target) => false;
    }

    /// <summary>
    /// Instance interface
    /// </summary>
    public interface IValidationPlugin
    {
        /// <summary>
        /// Check or get information need for validation (interactive)
        /// </summary>
        /// <param name="target"></param>
        void Aquire(Target target, IOptionsService optionsService, IInputService inputService);

        /// <summary>
        /// Check information need for validation (unattended)
        /// </summary>
        /// <param name="target"></param>
        void Default(Target target, IOptionsService optionsService);

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
