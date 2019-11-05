using System;
using System.IO;

namespace PKISharp.WACS.UnitTests.Infrastructure
{
    internal class Directory
    {
        public static DirectoryInfo Temp()
        {
            var tempPath = new DirectoryInfo(Environment.ExpandEnvironmentVariables("%TEMP%\\wacs-test"));
            if (!tempPath.Exists)
            {
                tempPath.Create();
            }
            return tempPath;
        }
    }
}
