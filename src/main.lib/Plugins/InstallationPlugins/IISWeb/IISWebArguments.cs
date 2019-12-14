namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWebArguments
    {
        public long? InstallationSiteId { get; set; }
        public int SSLPort { get; set; }
        public string? SSLIPAddress { get; set; }
    }
}
