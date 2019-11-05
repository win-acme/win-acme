namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal class HttpValidationArguments
    {
        public string WebRoot { get; set; }
        public bool Warmup { get; set; }
        public bool ManualTargetIsIIS { get; set; }
    }
}
