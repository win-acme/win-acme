using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple
{
    class Options
    {
        [Option(Default = "https://acme-v01.api.letsencrypt.org/", HelpText = "The address of the ACME server to use.")]
        public string BaseURI { get; set; }

        [Option(HelpText = "Accept the terms of service.")]
        public bool AcceptTOS { get; set; }

        [Option(HelpText = "Check for renewals.")]
        public bool Renew { get; set; }

        [Option(HelpText = "Overrides BaseURI setting to https://acme-staging.api.letsencrypt.org/")]
        public bool Test { get; set; }

        // can't easily make this a command line option since it would have to be saved
        //[Option(Default = 60f, HelpText = "Renewal period in days. Can be set to negative to test.")]
        //public float RenewalPeriod { get; set; } = 60;




        //[Option('r', "read", Required = true, HelpText = "Input files to be processed.")]
        //public IEnumerable<string> InputFiles { get; set; }

        //// Omitting long name, default --verbose
        //[Option(HelpText = "Prints all messages to standard output.")]
        //public bool Verbose { get; set; }

        //[Value(0, MetaName = "offset", HelpText = "File offset.")]
        //public long? Offset { get; set; }
    }
}
