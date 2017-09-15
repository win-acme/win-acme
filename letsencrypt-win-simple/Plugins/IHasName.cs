using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins
{
    public interface IHasName
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        string Name { get; }
    }
}
