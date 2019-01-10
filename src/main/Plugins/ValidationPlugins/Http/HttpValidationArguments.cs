namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    abstract class HttpValidationArguments
    {
        public string WebRoot { get; set; }
        public bool Warmup { get; set; }
        public bool ManualTargetIsIIS { get; set; }
    }
}
