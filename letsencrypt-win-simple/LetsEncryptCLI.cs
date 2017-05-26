using CommandLine;
using letsencrypt;
using letsencrypt.Support;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace LetsEncryptWinSimple
{
    class LetsEncryptCLI
    {
        static Options Options;
        static Plugin SelectedPlugin = null;

        static void Main(string[] args)
        {
            CreateLogger();
            ExtractFiles();
            try
            {
                ParseOptions(args);
                SelectedPlugin = LetsEncrypt.SelectPlugin(Options);
                if (SelectedPlugin != null && Validate())
                {
                    using (var client = LetsEncrypt.CreateAcmeClient(Options))
                    {
                        SelectedPlugin.client = client;
                        if (SelectedPlugin.SelectOptions(Options))
                        {
                            SelectedPlugin.AlternativeNames = GetAlternativeNames();
                            List<Target> targets = SelectedPlugin.GetTargets(Options);
                            foreach (Target target in targets)
                            {
                                InstallTarget(target);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(R.Anunhandledexceptionoccurred, e);
                Environment.ExitCode = 1;
            }
        }

        private static void ExtractFiles()
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string x64Path = Path.Combine(baseDir, "x64");
            string x86Path = Path.Combine(baseDir, "x86");
            CreateDir(x64Path);
            CreateDir(x86Path);
            ExtractFile(Path.Combine(x64Path, "libeay32.dll"), Properties.Resources.libeay64);
            ExtractFile(Path.Combine(x64Path, "ssleay32.dll"), Properties.Resources.ssleay64);
            ExtractFile(Path.Combine(x86Path, "libeay32.dll"), Properties.Resources.libeay32);
            ExtractFile(Path.Combine(x86Path, "ssleay32.dll"), Properties.Resources.ssleay32);
        }

        private static void ExtractFile(string filename, byte[] bytes)
        {
            if (!File.Exists(filename))
            {
                File.WriteAllBytes(filename, bytes);
            }
        }

        private static void InstallTarget(Target target)
        {
            if (Options.Renew)
            {
                SelectedPlugin.Renew(target, Options);
            }
            else
            {
                SelectedPlugin.Install(target, Options);
                string message = string.Format(R.Doyouwanttoautomaticallyrenewthiscertificate, Options.RenewalPeriod);
                if (LetsEncrypt.PromptYesNo(Options, message))
                {
                    LetsEncrypt.ScheduleRenewal(target, Options);
                }
            }
        }

        private static bool Validate()
        {
            bool IsElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            if (SelectedPlugin.RequiresElevated && !IsElevated)
            {
                Console.WriteLine(R.Thisprogrammustberunfromanadministrativecommandprompt);
                Environment.ExitCode = 2;
                return false;
            }
            return SelectedPlugin.Validate(Options);
        }

        internal static List<string> GetAlternativeNames()
        {
            var sanList = new List<string>();
            if (Options.San)
            {
                Console.Write(R.Enterallalternativenamesseparatedbycommas);
                Console.SetIn(new StreamReader(Console.OpenStandardInput(8192)));
                var input = Console.ReadLine();
                string[] alternativeNames = input.Split(',');

                if (alternativeNames.Length > 100)
                {
                    Log.Error(R.YouenteredtoomanyhostsforaSancertificate);
                }
                else
                {
                    sanList.AddRange(alternativeNames);
                }
            }
            return sanList;
        }

        private static void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole(outputTemplate: "{Message}{NewLine}{Exception}")
                .WriteTo.EventLog("letsencrypt_win_simple", restrictedToMinimumLevel: LogEventLevel.Warning)
                .ReadFrom.AppSettings()
                .CreateLogger();
        }

        private static void ParseOptions(string[] args)
        {
            try
            {
                ParserResult<Options> commandLineParseResult = Parser.Default.ParseArguments<Options>(args);
                Parsed<Options> parsed = commandLineParseResult as Parsed<Options>;
                if (parsed == null)
                {
                    Log.Error(R.Failedtoparsecommandline);
                    Environment.Exit(-1);
                }

                Options = parsed.Value;

                if (Options.Test)
                {
                    Options.BaseUri = "https://acme-staging.api.letsencrypt.org/";
                }

                if (string.IsNullOrWhiteSpace(Options.PFXPassword))
                {
                    Options.PFXPassword = Properties.Settings.Default.PFXPassword;
                }

                if (string.IsNullOrWhiteSpace(Options.PFXPassword))
                {
                    Options.PFXPassword = Properties.Settings.Default.PFXPassword;
                }

                if (Options.RSAKeyBits == 0)
                {
                    Options.RSAKeyBits = Properties.Settings.Default.RSAKeyBits;
                }

                if (string.IsNullOrWhiteSpace(Options.FileDateFormat))
                {
                    Options.FileDateFormat = Properties.Settings.Default.FileDateFormat;
                }
                
                ParseRenewalPeriod();

                ParseCertificateStore();

                ParseCentralSslStore();

                CreateConfigPath();

                SetAndCreateCertificatePath();

                Log.Debug("{@Options}", Options);
            }
            catch
            {
                Log.Error(R.Invalidcommandlineoptions);
                throw;
            }
        }

        private static void ParseRenewalPeriod()
        {
            if (Options.RenewalPeriod <= 0)
            {
                Options.RenewalPeriod = Properties.Settings.Default.RenewalDays;
            }
            Log.Information(R.Renewalperiod, Options.RenewalPeriod);
        }

        private static void ParseCertificateStore()
        {
            if (string.IsNullOrEmpty(Options.CertificateStore))
            {
                Options.CertificateStore = Properties.Settings.Default.CertificateStore;
            }
            Log.Information(R.Certificatestore, Options.CertificateStore);
        }

        private static void ParseCentralSslStore()
        {
            if (!string.IsNullOrWhiteSpace(Options.CentralSslStore))
            {
                Log.Information(R.UsingcentralizedSSLpath, Options.CentralSslStore);
                Options.CentralSsl = true;
            }
        }

        private static void CreateConfigPath()
        {
            if (string.IsNullOrEmpty(Options.ConfigPath))
            {
                Options.ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), LetsEncrypt.CLIENT_NAME,
                    LetsEncrypt.CleanFileName(Options.BaseUri));
            }
            Log.Information(R.Configpath, Options.ConfigPath);
            CreateDir(Options.ConfigPath);
        }

        private static void CreateDir(string path)
        {
            Directory.CreateDirectory(path);
        }

        private static void SetAndCreateCertificatePath()
        {
            if (string.IsNullOrWhiteSpace(Options.CertOutPath))
            {
                Options.CertOutPath = Options.ConfigPath;
            }
            CreateCertificatePath();
            Log.Information(R.Certificatefolder, Options.CertOutPath);
        }

        private static void CreateCertificatePath()
        {
            bool failed = true;
            try
            {
                if (!string.IsNullOrEmpty(Options.CertOutPath))
                {
                    CreateDir(Options.CertOutPath);
                }
                failed = false;
            }
            catch (Exception ex)
            {
                Log.Warning(
                    R.Errorcreatingthecertificatedirectory,
                    Options.CertOutPath, ex);
            }
            if (failed)
            {
                throw new DirectoryNotFoundException(R.Certificatedirectorycouldnotbecreated);
            }
        }
    }
}
