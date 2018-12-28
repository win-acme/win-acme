using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS
{
    public class Constants
    {
        public const int maxNames = 100;
    }

    public enum RunLevel
    {
        Unattended = 0,
        Simple = 10,
        Advanced = 20
    }

}
