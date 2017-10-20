using ACMESharp.ACME;
using System.IO;
using System.Linq;
using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Clients;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class IIS : FileSystem
    {
        public override string Name => nameof(IIS);
        public override string Description => "Create temporary application in IIS (recommended)";

        private IISClient _iisClient = new IISClient();

        public override void BeforeAuthorize(Target target, HttpChallenge challenge)
        {
            _iisClient.PrepareSite(target);
            base.BeforeAuthorize(target, challenge);
        }

        public override void BeforeDelete(Target target, HttpChallenge challenge)
        {
            _iisClient.UnprepareSite(target);
            base.BeforeDelete(target, challenge);
        }

        public override IValidationPlugin CreateInstance(Target target)
        {
            return new IIS();
        }

        public override bool CanValidate(Target target)
        {
            return target.IIS == true && target.SiteId > 0;
        }

        public override void Default(Options options, Target target) { }

        public override void Aquire(Options options, InputService input, Target target) { }
    }
}
