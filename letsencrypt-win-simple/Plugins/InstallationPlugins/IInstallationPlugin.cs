using Autofac;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    /// <summary>
    /// Handles configuration
    /// </summary>
    public interface IInstallationPluginFactory : IHasName
    {
        /// <summary>
        /// Can this plugin be used for this specific target?
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanInstall(ScheduledRenewal renewal);

        /// <summary>
        /// Check or get information need for installation (interactive)
        /// </summary>
        /// <param name="target"></param>
        void Aquire(IOptionsService options, IInputService input, ScheduledRenewal renewal);

        /// <summary>
        /// Check information need for installation (unattended)
        /// </summary>
        /// <param name="target"></param>
        void Default(IOptionsService options, ScheduledRenewal renewal);

        /// <summary>
        /// Which type is used as instance
        /// </summary>
        Type Instance { get; }
    }

    class NullInstallationFactory : IInstallationPluginFactory, IIsNull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IInstallationPluginFactory.Instance => typeof(object);
        void IInstallationPluginFactory.Aquire(IOptionsService options, IInputService input, ScheduledRenewal renewal) { }
        void IInstallationPluginFactory.Default(IOptionsService options, ScheduledRenewal renewal) { }
        bool IInstallationPluginFactory.CanInstall(ScheduledRenewal renewal) => false;
    }

    /// <summary>
    /// Does the actual work
    /// </summary>
    public interface IInstallationPlugin
    {
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
