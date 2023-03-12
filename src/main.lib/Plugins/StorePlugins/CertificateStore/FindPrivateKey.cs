using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    /// <summary>
    /// Based on Microsoft "FindPrivateKey" example
    /// </summary>
    partial class FindPrivateKey
    {
        private readonly ILogService _log;

        public FindPrivateKey(ILogService log) => _log = log;

        internal FileInfo? Find(X509Certificate2 cert)
        {
            string file;
            string dir;
            try
            {
                file = GetKeyFileName(cert);
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to find private key file name: {ex}", ex.Message);
                return null;
            }
            try
            {
                dir = GetKeyFileDirectory(file);
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to find private key folder: {ex}", ex.Message);
                return null;
            }
            return new FileInfo(Path.Combine(dir, file));
        }

        static string GetKeyFileName(X509Certificate2 cert)
        {
            var ecdsa = cert.GetECDsaPrivateKey();
            if (ecdsa is ECDsaCng ecdsaCng && !string.IsNullOrWhiteSpace(ecdsaCng.Key.UniqueName)) 
            { 
                return ecdsaCng.Key.UniqueName;
            }
            var rsa = cert.GetRSAPrivateKey();
            if (rsa is RSACng rsaCng && !string.IsNullOrWhiteSpace(rsaCng.Key.UniqueName))
            {
                return rsaCng.Key.UniqueName;
            }

            // Fallback using Win32 API magic, might not be needed at all anymore?

            var hProvider = IntPtr.Zero; // CSP handle
            var freeProvider = false; // Do we need to free the CSP ?
            uint acquireFlags = 0;
            var _keyNumber = 0;
            string? keyFileName = null;

            // Determine whether there is private key information available for this certificate in the key store
            if (CryptAcquireCertificatePrivateKey(cert.Handle, acquireFlags, IntPtr.Zero, ref hProvider, ref _keyNumber, ref freeProvider))
            {
                var pBytes = IntPtr.Zero; // Native Memory for the CRYPT_KEY_PROV_INFO structure
                var cbBytes = 0; // Native Memory size
                try
                {
                    if (CryptGetProvParam(hProvider, CryptGetProvParamType.PP_UNIQUE_CONTAINER, IntPtr.Zero, ref cbBytes, 0))
                    {
                        pBytes = Marshal.AllocHGlobal(cbBytes);

                        if (CryptGetProvParam(hProvider, CryptGetProvParamType.PP_UNIQUE_CONTAINER, pBytes, ref cbBytes, 0))
                        {
                            var keyFileBytes = new byte[cbBytes];
                            Marshal.Copy(pBytes, keyFileBytes, 0, cbBytes);
                            // Copy eveything except tailing null byte
                            keyFileName = Encoding.ASCII.GetString(keyFileBytes, 0, keyFileBytes.Length - 1);
                        }
                    }
                }
                finally
                {
                    if (freeProvider)
                    {
                        CryptReleaseContext(hProvider, 0);
                    }
                    if (pBytes != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pBytes);
                    }
                }
            }
            if (keyFileName == null)
            {
                throw new InvalidOperationException("Unable to obtain private key file name");
            }
            return keyFileName;
        }

        static string GetKeyFileDirectory(string keyFileName)
        {
            var common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var foldersToCheck = new List<string>
            {
                Path.Join(common, "\\Microsoft\\Crypto\\RSA\\MachineKeys"),
                Path.Join(common, "\\Microsoft\\Crypto\\Keys")
            };
            foreach (var folder in foldersToCheck)
            {
                var files = Directory.GetFiles(folder, keyFileName);
                if (files.Length > 0)
                {
                    return folder;
                }
            }
            throw new InvalidOperationException("Unable to locate private key file directory");
        }

        [LibraryImport("crypt32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CryptAcquireCertificatePrivateKey(IntPtr pCert, uint dwFlags, IntPtr pvReserved, ref IntPtr phCryptProv, ref int pdwKeySpec, [MarshalAs(UnmanagedType.Bool)] ref bool pfCallerFreeProv);

        [LibraryImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CryptGetProvParam(IntPtr hCryptProv, CryptGetProvParamType dwParam, IntPtr pvData, ref int pcbData, uint dwFlags);

        [LibraryImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CryptReleaseContext(IntPtr hProv, uint dwFlags);
    }

    enum CryptGetProvParamType
    {
        PP_ENUMALGS = 1,
        PP_ENUMCONTAINERS = 2,
        PP_IMPTYPE = 3,
        PP_NAME = 4,
        PP_VERSION = 5,
        PP_CONTAINER = 6,
        PP_CHANGE_PASSWORD = 7,
        PP_KEYSET_SEC_DESCR = 8,       // get/set security descriptor of keyset
        PP_CERTCHAIN = 9,      // for retrieving certificates from tokens
        PP_KEY_TYPE_SUBTYPE = 10,
        PP_PROVTYPE = 16,
        PP_KEYSTORAGE = 17,
        PP_APPLI_CERT = 18,
        PP_SYM_KEYSIZE = 19,
        PP_SESSION_KEYSIZE = 20,
        PP_UI_PROMPT = 21,
        PP_ENUMALGS_EX = 22,
        PP_ENUMMANDROOTS = 25,
        PP_ENUMELECTROOTS = 26,
        PP_KEYSET_TYPE = 27,
        PP_ADMIN_PIN = 31,
        PP_KEYEXCHANGE_PIN = 32,
        PP_SIGNATURE_PIN = 33,
        PP_SIG_KEYSIZE_INC = 34,
        PP_KEYX_KEYSIZE_INC = 35,
        PP_UNIQUE_CONTAINER = 36,
        PP_SGC_INFO = 37,
        PP_USE_HARDWARE_RNG = 38,
        PP_KEYSPEC = 39,
        PP_ENUMEX_SIGNING_PROT = 40,
        PP_CRYPT_COUNT_KEY_USE = 41,
    }
}
