using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    class HttpValidationParameters
    {
        public ISettingsService Settings { get; private set; }
        public Renewal Renewal { get; private set; }
        public TargetPart TargetPart { get; private set; }
        public RunLevel RunLevel { get; private set; }
        public string Identifier { get; private set; }
        public ILogService LogService { get; private set; }
        public IInputService InputService { get; private set; }
        public ProxyService ProxyService { get; private set; }

        public HttpValidationParameters(
            ILogService log, 
            IInputService input, 
            ISettingsService settings,
            ProxyService proxy,
            Renewal renewal, 
            TargetPart target, 
            RunLevel runLevel, 
            string identifier)
        {
            Renewal = renewal;
            TargetPart = target;
            RunLevel = runLevel;
            Identifier = identifier;
            Settings = settings;
            ProxyService = proxy;
            LogService = log;
            InputService = input;
        }
    }
}
