namespace LetsEncrypt.ACME.Simple.Services
{
    public interface IOptionsService
    {
        Options Options { get; set; }

        string TryGetOption(string providedValue, InputService input, string what, bool secret = false);
        string TryGetOption(string providedValue, InputService input, string[] what, bool secret = false);
        string TryGetRequiredOption(string optionName, string providedValue);
    }
}