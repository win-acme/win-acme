using System;

namespace PKISharp.WACS
{
    public class Constants
    {
        public const int maxNames = 100;
    }

    [Flags]
    public enum RunLevel
    {
        Unattended = 1,
        Interactive = 2,
        Simple = 4,
        Advanced = 8,
        Test = 16,
        Import = 32
    }

}
