namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindingsArguments
    {
        public string? SiteId { get; set; }
        public string? Host { get; set; }
        public string? Pattern { get; set; }
        public string? Regex { get; set; }
        public string? CommonName { get; set; }
        public string? ExcludeBindings { get; set; }
    }
}
