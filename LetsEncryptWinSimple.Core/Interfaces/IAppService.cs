using System.Collections.Generic;
using LetsEncryptWinSimple.Core.Configuration;

namespace LetsEncryptWinSimple.Core.Interfaces
{
    public interface IAppService
    {
        void LaunchApp();
        List<Target> GetTargetsSorted();
    }
}