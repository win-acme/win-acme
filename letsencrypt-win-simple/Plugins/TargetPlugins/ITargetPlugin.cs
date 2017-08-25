using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    interface ITargetPlugin
    {
        string Name { get; }
        Target Aquire { get; }
        Target Refresh(Target scheduled);
    }
}
