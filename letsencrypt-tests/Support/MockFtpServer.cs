using FubarDev.FtpServer;
using FubarDev.FtpServer.AccountManagement;
using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.FileSystem.DotNet;
using System.IO;
using System.Net;
using System.Reflection;

namespace letsencrypt_tests.Support
{
    public class MockFtpServer
    {
        private static FtpServer server;

        internal static string localPath;
        internal static IMembershipProvider membershipProvider;
        internal static IFileSystemClassFactory filesystemProvider;
        internal static IFtpCommandHandlerFactory commandHandler;

        public static void Start(int listenPort)
        {
            if (server != null)
            {
                Stop();
            }
            // allow any login
            membershipProvider = new MockFtpMembershipProvider();

            // use %TEMP%/TestFtpServer as root folder
            if (localPath == null) { localPath = Path.Combine(Path.GetTempPath(), "TestFtpServer"); }
            if (filesystemProvider == null) { filesystemProvider = new DotNetFileSystemProvider(localPath, false); }
            if (commandHandler == null) { commandHandler = new AssemblyFtpCommandHandlerFactory(typeof(FtpServer).GetTypeInfo().Assembly); }

            server = new FtpServer(filesystemProvider, membershipProvider, "127.0.0.1", listenPort, commandHandler);
            server.Start();
        }

        public static void Stop()
        {
            try
            {
                if (server != null)
                {
                    server.Stop();
                    server = null;
                }
            }
            catch { }
        }
    }
}
