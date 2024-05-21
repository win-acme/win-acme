using Org.BouncyCastle.Security;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS.Extensions
{
    public static class CertificateInfoExtensions
    {
        /// <summary>
        /// Get PFX archive as MemoryStream
        /// </summary>
        /// <param name="ci"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static MemoryStream PfxStream(this ICertificateInfo ci, string? password = null)
        {
            var stream = new MemoryStream();
            ci.Collection.Save(stream, (password ?? "").ToCharArray(), new SecureRandom());
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        /// <summary>
        /// Get PFX archive as byte array
        /// </summary>
        /// <param name="ci"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static byte[] PfxBytes(this ICertificateInfo ci, string? password = null) => PfxStream(ci, password).ToArray();

        /// <summary>
        /// Save PFX archive to disk location
        /// </summary>
        /// <param name="ci"></param>
        /// <param name="path"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<FileInfo> PfxSave(this ICertificateInfo ci, string path, string? password = null)
        {
            var fi = new FileInfo(path);
            using var fs = fi.Open(FileMode.Create);
            using var stream = PfxStream(ci, password);
            await stream.CopyToAsync(fs);
            await fs.FlushAsync();
            fi.Refresh();
            return fi;
        }

        /// <summary>
        /// Get archive as .NET object
        /// </summary>
        /// <param name="ci"></param>
        /// <returns></returns>
        public static X509Certificate2Collection AsCollection(this ICertificateInfo ci, X509KeyStorageFlags flags, string? password = null)
        {
            using var pfxStream = ci.PfxStream(password);
            using var pfxStreamReader = new BinaryReader(pfxStream);
            var tempPfx = new X509Certificate2Collection();
            tempPfx.Import(
                pfxStreamReader.ReadBytes((int)pfxStream.Length),
                password,
                flags);
            return tempPfx;
        }
    }
}
