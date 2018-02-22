namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// TargetPluginFactory interface
    /// </summary>
    public interface ITargetPluginFactory : IHasName, IHasType
    {
        /// <summary>
        /// Hide when it cannot be chosen
        /// </summary>
        bool Hidden { get; }
    }
}
