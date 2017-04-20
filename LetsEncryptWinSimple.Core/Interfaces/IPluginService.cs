using LetsEncryptWinSimple.Core.Configuration;

namespace LetsEncryptWinSimple.Core.Interfaces
{
    public interface IPluginService
    {
        void DefaultAction(Target target);
    }
}