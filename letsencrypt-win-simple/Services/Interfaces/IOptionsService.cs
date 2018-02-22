namespace PKISharp.WACS.Services
{
    public interface IOptionsService
    {
        Options Options { get; set; }

        string TryGetOption(string providedValue, IInputService input, string what, bool secret = false);
        string TryGetOption(string providedValue, IInputService input, string[] what, bool secret = false);
        string TryGetRequiredOption(string optionName, string providedValue);
        long? TryGetLong(string optionName, string providedValue);
    }
}