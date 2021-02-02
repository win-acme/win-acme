using System.IO;
using bc = Org.BouncyCastle;

namespace PKISharp.WACS.Services
{
    public class PemService
    {
        /// <summary>
        /// Helper function for PEM encoding
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string GetPem(object obj, string? password = null)
        {
            string pem;
            using (var tw = new StringWriter())
            {
                var pw = new bc.OpenSsl.PemWriter(tw);
                if (string.IsNullOrEmpty(password))
                {
                    pw.WriteObject(obj);
                } 
                else
                {
                    pw.WriteObject(obj, "AES-256-CBC", password.ToCharArray(), new bc.Security.SecureRandom());
                }
                pem = tw.GetStringBuilder().ToString();
                tw.GetStringBuilder().Clear();
            }
            return pem;
        }
        public string GetPem(string name, byte[] content) => GetPem(new bc.Utilities.IO.Pem.PemObject(name, content));

        /// <summary>
        /// Helper function for reading PEM encoding
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pem"></param>
        /// <returns></returns>
        public T? ParsePem<T>(string pem) where T: class
        {
            using var tr = new StringReader(pem);
            var pr = new bc.OpenSsl.PemReader(tr);
            return pr.ReadObject() as T;
        }
    }
}
