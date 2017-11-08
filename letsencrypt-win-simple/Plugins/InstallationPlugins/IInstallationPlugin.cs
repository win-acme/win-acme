using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    /// <summary>
    /// Handles configuration
    /// </summary>
    public interface IInstallationPluginFactory : IHasName, IHasType
    {
        /// <summary>
        /// Can this plugin be used for this specific target?
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanInstall(ScheduledRenewal renewal);
    }

    /// <summary>
    /// StorePluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class BaseInstallationPluginFactory<T> : BasePluginFactory<T>, IInstallationPluginFactory where T : IInstallationPlugin
    {
        public BaseInstallationPluginFactory(string name, string description) : base(name, description) { }
        public virtual bool CanInstall(ScheduledRenewal renewal) => true;
    }

    /// <summary>
    /// Null implementation
    /// </summary>
    class NullInstallationFactory : IInstallationPluginFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IHasType.Instance => typeof(object);
        bool IInstallationPluginFactory.CanInstall(ScheduledRenewal renewal) => false;
    }

    /// <summary>
    /// Does the actual work
    /// </summary>
    public interface IInstallationPlugin
    {
        /// <summary>
        /// Check or get information need for installation (interactive)
        /// </summary>
        /// <param name="target"></param>
        void Aquire();

        /// <summary>
        /// Check information need for installation (unattended)
        /// </summary>
        /// <param name="target"></param>
        void Default();

        /// <summary>
        /// Do the installation work
        /// </summary>
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="newCertificateInfo"></param>
        /// <param name="oldCertificateInfo"></param>
        void Install(CertificateInfo newCertificateInfo, CertificateInfo oldCertificateInfo);
    }
}
