using LetsEncrypt.ACME.Simple.Core.Configuration;

namespace LetsEncrypt.ACME.Simple.Core.Interfaces
{
    public interface IPluginService
    {
        void DefaultAction(Target target);
    }
}