using Org.BouncyCastle.Crypto;
using PKISharp.WACS.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PKISharp.WACS.DomainObjects
{
    [DebuggerDisplay("Target: {CommonName.Value}")]
    public class Target
    {
        public Target(string friendlyName, string commonName, IList<TargetPart> parts) : 
            this(friendlyName, new DnsIdentifier(commonName), parts) { }

        public Target(Identifier identifier) : 
            this(new List<Identifier> { identifier }) { }

        public Target(IEnumerable<Identifier> identifiers)
        {
            if (!identifiers.Any())
            {
                throw new ArgumentException("Should not be an empty collection", nameof(identifiers));
            }
            FriendlyName = identifiers.First().Value;
            CommonName = identifiers.First();
            Parts = new[] { new TargetPart(identifiers) };
        }

        public Target(string friendlyName, Identifier commonName, IList<TargetPart> parts)
        {
            FriendlyName = friendlyName;
            CommonName = commonName;
            Parts = parts;
        }

        /// <summary>
        /// Suggest a FriendlyName for the certificate,
        /// but this may be overruled by the user
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// CommonName for the certificate
        /// </summary>
        public Identifier CommonName { get; private set; }

        /// <summary>
        /// Different parts that make up this target
        /// </summary>
        public IList<TargetPart> Parts { get; private set; }

        /// <summary>
        /// Check if all parts are IIS
        /// </summary>
        public bool IIS => Parts.All(x => x.IIS);

        /// <summary>
        /// The CSR provided by the user
        /// </summary>
        public IEnumerable<byte>? UserCsrBytes { get; set; }

        /// <summary>
        /// The CSR used to request the certificate
        /// </summary>
        public IEnumerable<byte>? CsrBytes { get; set; }

        /// <summary>
        /// The Private Key corresponding to the CSR
        /// </summary>
        public AsymmetricKeyParameter? PrivateKey { get; set; }

        /// <summary>
        /// Pretty print information about the target
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var x = new StringBuilder();
            x.Append(CommonName.Value);
            var alternativeNames = Parts.SelectMany(p => p.Identifiers).Distinct();
            if (alternativeNames.Count() > 1)
            {
                _ = x.Append($" and {alternativeNames.Count() - 1} alternative{(alternativeNames.Count() > 1 ? "s" : "")}");
            }
            return x.ToString();
        }
    }
}