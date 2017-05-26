using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace letsencrypt_tests.Support
{
    internal class Settings
    {
        private static TestContext testContext;

        public static int HTTPProxyPort { get { return Convert.ToInt32(AppSetting("ProxyPort", "22233")); } }

        public static int FTPProxyPort { get { return Convert.ToInt32(AppSetting("ProxyPort", "21212")); } }

        private static string AppSetting(string name, string defaultValue = null)
        {
            return ContextSetting(name) ?? ConfigurationManager.AppSettings[name] ?? defaultValue;
        }

        private static string ContextSetting(string name, string defaultValue = null)
        {
            if (testContext != null && testContext.Properties[name] != null)
            {
                return Convert.ToString(testContext.Properties[name]);
            }
            return defaultValue;
        }

        public static void Initialize(TestContext context)
        {
            testContext = context;
        }
    }
}