// Credit: http://btburnett.com/2009/05/create-a-self-signed-ssl-certificate-in-net.html

using System;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace letsencrypt_tests.Support
{
    public sealed class CertificateCreator
    {

        #region Interop

        #region Helpers

        internal sealed class SafeCryptProvHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            private SafeCryptProvHandle() : base(true)
            {
            }

            internal SafeCryptProvHandle(IntPtr handle) : base(true)
            {
                base.SetHandle(handle);
            }

            [SuppressUnmanagedCodeSecurity(), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("advapi32.dll", SetLastError = true)]
            private static extern bool CryptReleaseContext(IntPtr hCryptProv, UInt32 dwFlags);

            protected override bool ReleaseHandle()
            {
                return SafeCryptProvHandle.CryptReleaseContext(base.handle, 0);
            }

            // Properties
            static internal SafeCryptProvHandle InvalidHandle
            {
                get { return new SafeCryptProvHandle(IntPtr.Zero); }
            }

        }

        private sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            private SafeCertContextHandle() : base(true)
            {
            }

            internal SafeCertContextHandle(IntPtr handle) : base(true)
            {
                base.SetHandle(handle);
            }

            [SuppressUnmanagedCodeSecurity(), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("crypt32.dll", SetLastError = true)]
            private static extern bool CertFreeCertificateContext(IntPtr pCertContext);

            protected override bool ReleaseHandle()
            {
                return SafeCertContextHandle.CertFreeCertificateContext(base.handle);
            }

            // Properties
            static internal SafeCertContextHandle InvalidHandle
            {
                get { return new SafeCertContextHandle(IntPtr.Zero); }
            }

            public CERT_CONTEXT ToStructure()
            {
                if (IsInvalid)
                {
                    throw new InvalidOperationException("SafeCertContextHandle Is Invalid");
                }

                return Marshal.PtrToStructure<CERT_CONTEXT>(handle);
            }

        }

        private sealed class SafeCryptKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            private SafeCryptKeyHandle() : base(true)
            {
            }

            internal SafeCryptKeyHandle(IntPtr handle) : base(true)
            {
                base.SetHandle(handle);
            }

            [SuppressUnmanagedCodeSecurity(), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("advapi32.dll", SetLastError = true)]
            private static extern bool CryptDestroyKey(IntPtr hKey);

            protected override bool ReleaseHandle()
            {
                return SafeCryptKeyHandle.CryptDestroyKey(base.handle);
            }

            // Properties
            static internal SafeCertContextHandle InvalidHandle
            {
                get { return new SafeCertContextHandle(IntPtr.Zero); }
            }

        }

        private sealed class SafeCertStoreHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            private SafeCertStoreHandle() : base(true)
            {
            }

            internal SafeCertStoreHandle(IntPtr handle) : base(true)
            {
                base.SetHandle(handle);
            }

            [SuppressUnmanagedCodeSecurity(), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("crypt32.dll", SetLastError = true)]
            private static extern bool CertCloseStore(IntPtr hCertStore, UInt32 dwFlags);

            protected override bool ReleaseHandle()
            {
                return SafeCertStoreHandle.CertCloseStore(base.handle, 0);
            }

            // Properties
            static internal SafeCertStoreHandle InvalidHandle
            {
                get { return new SafeCertStoreHandle(IntPtr.Zero); }
            }

        }

        #endregion

        #region Constants


        private const UInt32 AT_KEYEXCHANGE = 1;
        private const UInt32 CERT_KEY_PROV_INFO_PROP_ID = 2;
        private const UInt32 CERT_STORE_ADD_REPLACE_EXISTING = 3;
        private const UInt32 CERT_STORE_PROV_SYSTEM = 10;
        private const UInt32 CERT_STORE_PROV_MEMORY = 2;
        private const UInt32 CERT_SYSTEM_STORE_LOCAL_MACHINE = 0x20000;
        private const UInt32 CRYPT_EXPORTABLE = 1;
        private const UInt32 CRYPT_MACHINE_KEYSET = 0x20;
        private const UInt32 CRYPT_NEWKEYSET = 8;
        private const UInt32 CRYPT_SILENT = 0x40;
        private const string MS_STRONG_PROV = "Microsoft Strong Cryptographic Provider";
        private const UInt32 PROV_RSA_FULL = 1;

        private const string szOID_RSA_MD5RSA = "1.2.840.113549.1.1.4";
        #endregion

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public short wYear;
            public short wMonth;
            public short wDayOfWeek;
            public short wDay;
            public short wHour;
            public short wMinute;
            public short wSecond;
            public short wMilliseconds;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CRYPT_KEY_PROV_INFO
        {
            internal string pwszContainerName;
            internal string pwszProvName;
            internal UInt32 dwProvType;
            internal UInt32 dwFlags;
            internal UInt32 cProvParam;
            internal IntPtr rgProvParam;
            internal UInt32 dwKeySpec;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CERT_CONTEXT
        {
            public UInt32 dwCertEncodingType;
            public IntPtr pbCertEncoded;
            public UInt32 cbCertEncoded;
            public IntPtr pCertInfo;
            public IntPtr hCertStore;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CERT_INFO
        {
            public UInt32 dwVersion;
            public CRYPTOAPI_BLOB SerialNumber;
            public CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;
            public CRYPTOAPI_BLOB Issuer;
            public System.Runtime.InteropServices.ComTypes.FILETIME NotBefore;
            public System.Runtime.InteropServices.ComTypes.FILETIME NotAfter;
            public CRYPTOAPI_BLOB Subject;
            public CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;
            public CRYPT_BIT_BLOB IssuerUniqueId;
            public CRYPT_BIT_BLOB SubjectUniqueId;
            public UInt32 cExtension;
            public IntPtr rgExtension;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CRYPT_BIT_BLOB
        {
            public UInt32 cbData;
            public IntPtr pbData;
            public UInt32 cUnusedBits;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CRYPTOAPI_BLOB
        {
            public UInt32 cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CERT_PUBLIC_KEY_INFO
        {
            public CRYPT_ALGORITHM_IDENTIFIER Algorithm;
            public CRYPT_BIT_BLOB PublicKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CRYPT_ALGORITHM_IDENTIFIER
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pszObjId;
            public CRYPTOAPI_BLOB Parameters;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CERT_EXTENSION
        {
            [MarshalAs(UnmanagedType.LPStr)]
            internal string pszObjId;
            internal bool fCritical;
            internal CRYPTOAPI_BLOB Value;
        }

        #endregion

        #region Functions

        [DllImport("advapi32.dll", EntryPoint = "CryptAcquireContextA", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CryptAcquireContext([In(), Out()]
ref SafeCryptProvHandle hCryptProv, [In(), MarshalAs(UnmanagedType.LPStr)]
string pszContainer, [In(), MarshalAs(UnmanagedType.LPStr)]
string pszProvider, [In()]
UInt32 dwProvType, [In()]
UInt32 dwFlags);

        [DllImport("crypt32.dll", SetLastError = true)]
        private static extern SafeCertContextHandle CertCreateSelfSignCertificate(SafeCryptProvHandle hCryptProvOrNCryptKey, [In()]
ref CRYPTOAPI_BLOB pSubjectIssuerBlob, int dwFlags, [In()]
ref CRYPT_KEY_PROV_INFO pKeyProvInfo, [In()]
ref CRYPT_ALGORITHM_IDENTIFIER pSignatureAlgorithm, [In()]
ref SYSTEMTIME pStartTime, [In()]
ref SYSTEMTIME pEndTime, [MarshalAs(UnmanagedType.LPArray), In()]
CERT_EXTENSION[] pExtensions);


        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CryptGenKey(SafeCryptProvHandle hProv, int Algid, int dwFlags, ref SafeCryptKeyHandle phKey);

        [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeCertStoreHandle CertOpenStore([In()]
IntPtr lpszStoreProvider, [In()]
UInt32 dwMsgAndCertEncodingType, [In()]
IntPtr hCryptProv, [In()]
UInt32 dwFlags, [In()]
string pvPara);

        [DllImport("crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CertAddCertificateContextToStore([In()]
SafeCertStoreHandle hCertStore, [In()]
SafeCertContextHandle pCertContext, [In()]
UInt32 dwAddDisposition, [In(), Out()]
ref SafeCertContextHandle ppStoreContext);

        [DllImport("crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CertSetCertificateContextProperty(SafeCertContextHandle certificateContext, int propertyId, int flags, [In()]
ref CRYPT_KEY_PROV_INFO data);

        #endregion

        #endregion

        public static X509Certificate2 CreateSelfSignedCertificate(X500DistinguishedName distinguishedName, DateTime startDate, DateTime endDate)
        {
            byte[] nameData = distinguishedName.RawData;
            GCHandle dataHandle = GCHandle.Alloc(nameData, GCHandleType.Pinned);
            try
            {
                CRYPTOAPI_BLOB subjectBlob = default(CRYPTOAPI_BLOB);
                subjectBlob.cbData = (uint)nameData.Length;
                subjectBlob.pbData = dataHandle.AddrOfPinnedObject();

                string container = Guid.NewGuid().ToString();
                SafeCryptProvHandle context = new SafeCryptProvHandle(IntPtr.Zero);
                if (!CryptAcquireContext(ref context, container, MS_STRONG_PROV, PROV_RSA_FULL, CRYPT_NEWKEYSET | CRYPT_MACHINE_KEYSET | CRYPT_SILENT))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                try
                {
                    SafeCryptKeyHandle keyPtr = new SafeCryptKeyHandle(IntPtr.Zero);
                    if (!CryptGenKey(context, (int)AT_KEYEXCHANGE, (int)((2048 << 16) | CRYPT_EXPORTABLE), ref keyPtr))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    keyPtr.Dispose();

                    SYSTEMTIME sysStartTime = new SYSTEMTIME
                    {
                        wMonth = (short)startDate.Month,
                        wDay = (short)startDate.Day,
                        wYear = (short)startDate.Year,
                        wDayOfWeek = (short)startDate.DayOfWeek,
                        wHour = (short)startDate.Hour,
                        wMinute = (short)startDate.Minute,
                        wSecond = (short)startDate.Second,
                        wMilliseconds = (short)startDate.Minute
                    };

                    SYSTEMTIME sysEndTime = new SYSTEMTIME
                    {
                        wMonth = (short)endDate.Month,
                        wDay = (short)endDate.Day,
                        wYear = (short)endDate.Year,
                        wDayOfWeek = (short)endDate.DayOfWeek,
                        wHour = (short)endDate.Hour,
                        wMinute = (short)endDate.Minute,
                        wSecond = (short)endDate.Second,
                        wMilliseconds = (short)endDate.Minute
                    };

                    CRYPT_KEY_PROV_INFO keyInfo = new CRYPT_KEY_PROV_INFO();
                    keyInfo.pwszContainerName = container;
                    keyInfo.pwszProvName = MS_STRONG_PROV;
                    keyInfo.dwProvType = PROV_RSA_FULL;
                    keyInfo.dwKeySpec = AT_KEYEXCHANGE;
                    keyInfo.dwFlags = CRYPT_MACHINE_KEYSET;

                    CRYPT_ALGORITHM_IDENTIFIER alg = new CRYPT_ALGORITHM_IDENTIFIER();
                    alg.pszObjId = szOID_RSA_MD5RSA;

                    SafeCertContextHandle certContextHandle = CertCreateSelfSignCertificate(context, ref subjectBlob, 0, ref keyInfo, ref alg, ref sysStartTime, ref sysEndTime, null);
                    if (certContextHandle.IsInvalid)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    try
                    {
                        //SafeCertStoreHandle certStore = CertOpenStore(new IntPtr(CERT_STORE_PROV_SYSTEM), 0, IntPtr.Zero, CERT_SYSTEM_STORE_LOCAL_MACHINE, "My");
                        SafeCertStoreHandle certStore = CertOpenStore(new IntPtr(CERT_STORE_PROV_MEMORY), 0, IntPtr.Zero, 0, null);
                        if (certStore.IsInvalid)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                        try
                        {
                            SafeCertContextHandle storeCertContext = new SafeCertContextHandle(IntPtr.Zero);
                            if (!CertAddCertificateContextToStore(certStore, certContextHandle, CERT_STORE_ADD_REPLACE_EXISTING, ref storeCertContext))
                            {
                                throw new Win32Exception(Marshal.GetLastWin32Error());
                            }
                            try
                            {
                                if (!CertSetCertificateContextProperty(storeCertContext, (int)CERT_KEY_PROV_INFO_PROP_ID, 0, ref keyInfo))
                                {
                                    throw new Win32Exception(Marshal.GetLastWin32Error());
                                }

                                CERT_CONTEXT certContext = storeCertContext.ToStructure();
                                byte[] buffer = new byte[certContext.cbCertEncoded];
                                Marshal.Copy(certContext.pbCertEncoded, buffer, 0, (int)certContext.cbCertEncoded);
                                return new X509Certificate2(buffer);
                            }
                            finally
                            {
                                storeCertContext.Dispose();
                            }
                        }
                        finally
                        {
                            certStore.Dispose();
                        }
                    }
                    finally
                    {
                        certContextHandle.Dispose();
                    }
                }
                finally
                {
                    context.Dispose();
                }
            }
            finally
            {
                dataHandle.Free();
            }
        }

    }
}
