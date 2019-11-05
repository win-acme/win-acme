namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslArguments
    {
        public bool KeepExisting { get; set; }
        public string CentralSslStore { get; set; }
        public string PfxPassword { get; set; }
    }
}
