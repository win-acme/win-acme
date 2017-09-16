using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    class Azure : DnsValidation
    {
        public override string Name
        {
            get
            {
                return "Dns-Azure";
            }
        }

        public override void CreateRecord(string recordName, string token)
        {
            throw new NotImplementedException();
        }

        public override void DeleteRecord(string recordName)
        {
            throw new NotImplementedException();
        }
    }
}
