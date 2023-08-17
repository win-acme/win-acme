using PKISharp.WACS.Configuration;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class CloudDnsOptionsFactory : PluginOptionsFactory<CloudDnsOptions>
    {
        private readonly ArgumentsInputService _arguments;
        private readonly ILogService _log;

        public CloudDnsOptionsFactory(ArgumentsInputService arguments, ILogService log)
        {
            _arguments = arguments;
            _log = log;
        }

        private ArgumentResult<string?> ServiceAccountKey => _arguments.
            GetString<CloudDnsArguments>(a => a.ServiceAccountKey).
            Validate(x => Task.FromResult(x.ValidFile(_log)), "invalid path").
            Required();

        private ArgumentResult<string?> ProjectId => _arguments.
            GetString<CloudDnsArguments>(a => a.ProjectId).
            Required();

        public override async Task<CloudDnsOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            return new CloudDnsOptions
            {
                ServiceAccountKeyPath = await ServiceAccountKey.Interactive(input, "Path to Service Account Key").GetValue(),
                ProjectId = await ProjectId.Interactive(input).GetValue()
            };
        }


        public override async Task<CloudDnsOptions?> Default()
        {
            return new CloudDnsOptions
            {
                ServiceAccountKeyPath = await ServiceAccountKey.GetValue(),
                ProjectId = await ProjectId.GetValue()
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(CloudDnsOptions options)
        {
            yield return (ServiceAccountKey.Meta, options.ServiceAccountKeyPath);
            yield return (ProjectId.Meta, options.ProjectId);
        }
    }
}
