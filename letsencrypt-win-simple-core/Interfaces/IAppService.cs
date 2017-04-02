using System.Collections.Generic;
using LetsEncrypt.ACME.Simple.Core.Configuration;

namespace LetsEncrypt.ACME.Simple.Core.Interfaces
{
    public interface IAppService
    {
        void LaunchApp();
        List<Target> GetTargetsSorted();
    }
}