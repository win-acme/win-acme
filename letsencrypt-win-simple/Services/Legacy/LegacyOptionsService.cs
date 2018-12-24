namespace PKISharp.WACS.Services.Legacy
{
    public class LegacyOptionsService : IOptionsService
    {
        public Options Options { get => 
                throw new System.NotImplementedException();
        }

        public long? TryGetLong(string optionName, string providedValue)
        {
            throw new System.NotImplementedException();
        }

        public string TryGetOption(string providedValue, IInputService input, string what, bool secret = false)
        {
            throw new System.NotImplementedException();
        }

        public string TryGetOption(string providedValue, IInputService input, string[] what, bool secret = false)
        {
            throw new System.NotImplementedException();
        }

        public string TryGetRequiredOption(string optionName, string providedValue)
        {
            throw new System.NotImplementedException();
        }
    }
}