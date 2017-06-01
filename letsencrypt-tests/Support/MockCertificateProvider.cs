using System;
using System.Collections.Generic;
using System.IO;
using ACMESharp.PKI;
using ACMESharp.PKI.Providers;

namespace letsencrypt_tests.Support
{
    public class MockCertificateProvider : OpenSslLibProvider
    {
        public MockCertificateProvider(IReadOnlyDictionary<string, string> newParams) : base(newParams)
        {
        }

        public override void ExportArchive(PrivateKey pk, IEnumerable<Crt> certs, ArchiveFormat fmt, Stream target, string password = "")
        {
        }

        public override void ExportCertificate(Crt cert, EncodingFormat fmt, Stream target)
        {
        }

        public override void ExportCsr(Csr csr, EncodingFormat fmt, Stream target)
        {
        }

        public override void ExportPrivateKey(PrivateKey pk, EncodingFormat fmt, Stream target)
        {
        }

        public override Csr GenerateCsr(CsrParams csrParams, PrivateKey pk, Crt.MessageDigest md)
        {
            return base.GenerateCsr(csrParams, pk, md);
        }

        public override PrivateKey GeneratePrivateKey(PrivateKeyParams pkp)
        {
            return base.GeneratePrivateKey(pkp);
        }

        public override Crt ImportCertificate(EncodingFormat fmt, Stream source)
        {
            return new Crt();
        }

        public override Csr ImportCsr(EncodingFormat fmt, Stream source)
        {
            return base.ImportCsr(fmt, source);
        }

        public override PrivateKey ImportPrivateKey<PK>(EncodingFormat fmt, Stream source)
        {
            return base.ImportPrivateKey<PK>(fmt, source);
        }
    }
}