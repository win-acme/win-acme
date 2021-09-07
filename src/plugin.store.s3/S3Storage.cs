using Amazon.Runtime;
using Amazon.S3.Model;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class S3Storage : IStorePlugin
    {
        private readonly S3StorageOptions _options;
        private readonly ILogService _log;

        public S3Storage(S3StorageOptions options, ILogService log)
        {
            _options = options;
            _log = log;
        }

        public (bool, string) Disabled => (false, "");

        public Task Delete(CertificateInfo certificateInfo) => Task.CompletedTask;

        public async Task Save(CertificateInfo certificateInfo)
        {
            try
            {
                //AWSCredentials credential;
                //if (_options.AccessKey != null && _options.SecretKey != null)
                //    credential = new BasicAWSCredentials(_options.AccessKey.Value, _options.SecretKey.Value);
                //else
                //    credential = new InstanceProfileAWSCredentials();

                _log.Information("Upload {0} to s3://{1}/{2}", certificateInfo.CommonName.Value, _options.Bucket, _options.FileKey);
                using (var s3Client = new Amazon.S3.AmazonS3Client())
                {
                    PutObjectRequest request = new PutObjectRequest
                    {
                        FilePath = certificateInfo.CacheFile!.FullName,
                        Key = _options.FileKey,
                        BucketName = _options.Bucket,
                        CannedACL = Amazon.S3.S3CannedACL.PublicRead
                    };

                    var response = await s3Client.PutObjectAsync(request);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error importing certificate to S3");
            }
        }
    }
}
