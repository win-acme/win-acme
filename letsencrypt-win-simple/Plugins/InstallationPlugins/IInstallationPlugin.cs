using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    interface IInstallationPlugin : IHasName
    {
        /// <summary>
        /// Do the installation work
        /// </summary>
        /// <param name="target"></param>
        /// <param name="challenge"></param>
        /// <param name="identifier"></param>
        /// <param name="options"></param>
        /// <param name="input"></param>
        void Install(ScheduledRenewal target, CertificateInfo certificateInfo);

        /// <summary>
        /// Can this plugin be used for this specific target?
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanInstall(ScheduledRenewal target);

        /// <summary>
        /// Create target-specific instance of the installation-plugin
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        IInstallationPlugin CreateInstance(ScheduledRenewal target);

        /// <summary>
        /// Check or get information need for installation (interactive)
        /// </summary>
        /// <param name="target"></param>
        void Aquire(IOptionsService options, IInputService input, ScheduledRenewal target);

        /// <summary>
        /// Check information need for installation (unattended)
        /// </summary>
        /// <param name="target"></param>
        void Default(IOptionsService options, ScheduledRenewal target);
    }
}
