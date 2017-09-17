using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    class Azure : DnsValidation
    {
        public override string Name => nameof(Azure);
        public override string Description => "Azure";

        public override void CreateRecord(Options options, Target target, string recordName, string token)
        {
            throw new NotImplementedException();
        }

        public override void DeleteRecord(Options options, Target target, string recordName)
        {
            throw new NotImplementedException();
        }

        public override void Aquire(Options options, InputService input, Target target)
        {
            target.AzureOptions = new AzureOptions(options, input);
        }

        public override void Default(Options options, Target target)
        {
            target.AzureOptions = new AzureOptions(options);
        }
    }
}
