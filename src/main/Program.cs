using Autofac;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PKISharp.WACS.Host
{
    /// <summary>
    /// This class serves as bootstrapper to call the main library
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Prevent starting program twice at the same time
        /// </summary>
        private static Mutex? _globalMutex;

        private static bool Verbose { get; set; }

        private async static Task Main(string[] args)
        {
            // Error handling
            AppDomain.CurrentDomain.UnhandledException += 
                new UnhandledExceptionEventHandler(OnUnhandledException);

            // Are we running in verbose mode?
            Verbose = args.Contains("--verbose");

            // The main class might change the character encoding
            // save the original setting so that it can be restored
            // after the run.
            var original = Console.OutputEncoding;
            try
            {
                // Setup IOC container
                var container = Autofac.GlobalScope(args, Verbose);
                AllowInstanceToRun(container);
                var wacs = container.Resolve<Wacs>();
                Environment.ExitCode = await wacs.Start().ConfigureAwait(false);
            } 
            catch (Exception ex)
            {
                Console.WriteLine(" Error in main function: " + ex.Message);
                if (Verbose)
                {
                    Console.WriteLine(ex.StackTrace);
                    while (ex.InnerException != null) {
                        ex = ex.InnerException;
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                FriendlyClose();
            } 
            finally
            {
                // Restore original code page
                Console.OutputEncoding = original;
                _globalMutex?.Dispose();
            }
        }

        /// <summary>
        /// Block multiple instances from running at the same time
        /// on the same configuration path, because they might 
        /// overwrite eachothers stuff
        /// </summary>
        /// <returns></returns>
        static void AllowInstanceToRun(ILifetimeScope container)
        {
            var logger = container.Resolve<ILogService>();
            _globalMutex = new Mutex(true, "wacs.exe", out var created);
            if (!created)
            {
                logger.Warning("Another instance of wacs.exe is already running, waiting for that to close...");
                try
                {
                    _ = _globalMutex.WaitOne();
                } 
                catch (AbandonedMutexException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Close in a friendly way
        /// </summary>
        static void FriendlyClose()
        {
            _globalMutex?.ReleaseMutex();
            Environment.ExitCode = -1;
            if (Environment.UserInteractive)
            {
                Console.WriteLine(" Press <Enter> to close");
                _ = Console.ReadLine();
            }
        }

        /// <summary>
        /// Final resort to catch unhandled exceptions and log something
        /// before the runtime explodes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Console.WriteLine(" Unhandled exception caught: " + ex.Message);
        }
    }
}