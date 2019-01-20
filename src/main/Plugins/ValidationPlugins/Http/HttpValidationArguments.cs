namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    class HttpValidationArguments
    {
        public string WebRoot { get; set; }
        public bool Warmup { get; set; }
        public bool ManualTargetIsIIS { get; set; }
    }
}
