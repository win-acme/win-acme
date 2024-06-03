using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System;
using System.IO;

namespace PKISharp.WACS.Services
{
    /// <summary>
    /// Wrapper class to keep track of configured protection mode
    /// </summary>
    public class PfxWrapper
    {
        public Pkcs12Store Store { get; private set; }
        public PfxProtectionMode ProtectionMode { get; private set; }

        public PfxWrapper(Pkcs12Store store, PfxProtectionMode protectionMode)
        {
            Store = store;
            ProtectionMode = protectionMode;
        }
    }

    /// <summary>
    /// Available protection modes
    /// </summary>
    public enum PfxProtectionMode
    {
        Default,
        Legacy,
        Aes256
    }

    public class PfxService
    {
        /// <summary>
        /// Helper function to create a new PfxWrapper with certain settings
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static PfxWrapper GetPfx(PfxProtectionMode protectionMode = PfxProtectionMode.Default)
        {
            var outputBuilder = new Pkcs12StoreBuilder();
            if (protectionMode == PfxProtectionMode.Default) 
            {
                // Windows Server 2019 and above
                protectionMode = 
                    Environment.OSVersion.Version.Build >= 17763 ? 
                    PfxProtectionMode.Aes256 : 
                    PfxProtectionMode.Legacy;
            }
            if (protectionMode == PfxProtectionMode.Aes256)
            {
                outputBuilder.SetKeyAlgorithm(
                    NistObjectIdentifiers.IdAes256Cbc,
                    PkcsObjectIdentifiers.IdHmacWithSha256);
            }
            return new PfxWrapper(outputBuilder.Build(), protectionMode);
        }

        /// <summary>
        /// Helper function to create a new PfxWrapper with different PfxProtectionMode
        /// setting
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static PfxWrapper ConvertPfx(PfxWrapper original, PfxProtectionMode protection)
        {
            if (original.ProtectionMode == protection) 
            {
                return original;
            }
            var stream = new MemoryStream();
            var password = PasswordGenerator.Generate().ToCharArray();
            original.Store.Save(stream, password, new SecureRandom());
            stream.Seek(0, SeekOrigin.Begin);
            var ret = GetPfx(protection);
            ret.Store.Load(stream, password);
            return ret;
        }
    }
}
