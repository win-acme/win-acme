using System.IO;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;
using openssl = Org.BouncyCastle.OpenSsl;

namespace PKISharp.WACS.Services
{
    public class PemService
    {
        /// <summary>
        /// Helper function for PEM encoding
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetPem(object obj, string? password = null)
        {
            string pem;
            using (var tw = new StringWriter())
            {
                var pw = new openssl.PemWriter(tw);
                if (string.IsNullOrEmpty(password))
                {
                    pw.WriteObject(obj);
                } 
                else
                {
                    pw.WriteObject(obj, "AES-256-CBC", password.ToCharArray(), new SecureRandom());
                }
                pem = tw.GetStringBuilder().ToString();
                tw.GetStringBuilder().Clear();
            }
            return pem;
        }

        /// <summary>
        /// Helper for content that's already byte encoded
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string GetPem(string name, byte[] content) => GetPem(new PemObject(name, content));

        /// <summary>
        /// Helper function for reading PEM encoding
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pem"></param>
        /// <returns></returns>
        public static T? ParsePem<T>(string pem) where T: class
        {
            using var tr = new StringReader(pem);
            var pr = new openssl.PemReader(tr);
            return pr.ReadObject() as T;
        }
    }
}
