using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISOptionsFactory : InstallationPluginFactory<IIS, IISOptions>
    {
        public override int Order => 5;
        private readonly IIISClient _iisClient;
        private readonly ArgumentsInputService _arguments;

        public IISOptionsFactory(IIISClient iisClient, ArgumentsInputService arguments, IUserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _arguments = arguments;
            Disabled = IIS.Disabled(userRoleService, iisClient);
        }

        public override (bool, string?) CanInstall(IEnumerable<Type> storeTypes, IEnumerable<Type> installationTypes)
        {
            if (installationTypes.Contains(typeof(IIS)))
            {
                return (false, "Cannot be used more than once in a renewal.");
            }
            if (storeTypes.Contains(typeof(CertificateStore)) || storeTypes.Contains(typeof(CentralSsl))) 
            {
                return (true, null);
            }
            return (false, "Requires CertificateStore or CentralSsl store plugin.");
        }
    
        private ArgumentResult<int?> NewBindingPort => _arguments.
            GetInt<IISArguments>(x => x.SSLPort).
            WithDefault(IISClient.DefaultBindingPort).
            DefaultAsNull().
            Validate(x => Task.FromResult(x >= 1), "invalid port").
            Validate(x => Task.FromResult(x <= 65535), "invalid port");

        private ArgumentResult<string?> NewBindingIp => _arguments.
            GetString<IISArguments>(x => x.SSLIPAddress).
            WithDefault(IISClient.DefaultBindingIp).
            DefaultAsNull().
            Validate(x => Task.FromResult(x == "*" || IPAddress.Parse(x!) != null), "invalid address");

        private ArgumentResult<long?> InstallationSite => _arguments.
            GetLong<IISArguments>(x => x.InstallationSiteId).
            Validate(x => Task.FromResult(_iisClient.GetSite(x!.Value) != null), "invalid site");

        private ArgumentResult<long?> FtpSite => _arguments.
            GetLong<IISArguments>(x => x.FtpSiteId).
            Validate(x => Task.FromResult(_iisClient.GetSite(x!.Value) != null), "invalid site").
            Validate(x => Task.FromResult(_iisClient.GetSite(x!.Value).Type == IISSiteType.Ftp), "not an ftp site");

        public override async Task<IISOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISOptions()
            {
                NewBindingPort = await NewBindingPort.GetValue(),
                NewBindingIp = await NewBindingIp.GetValue()
            };
            inputService.Show(null,
                   "This plugin will update *all* binding using the previous certificate in both Web and " +
                   "FTP sites, regardless of whether those bindings were created manually or by the program " +
                   "itself. Therefor you'll never need to run this installation step twice.");
       
            var ask = true;
            if (target.IIS)
            {
                inputService.CreateSpace();
                inputService.Show(null,
                    "During initial setup, it will try to make as few changes as possible to IIS to cover " +
                    "the source hosts. If new bindings are needed, by default it will create those at " +
                    "the same site where the HTTP binding for that host was found.");
                ask = runLevel.HasFlag(RunLevel.Advanced) && 
                    await inputService.PromptYesNo("Create new bindings in a different site?", false);
            } 
            if (ask)
            {
                var chosen = await inputService.ChooseRequired("Choose site to create new bindings",
                   _iisClient.Sites,
                   x => Choice.Create(x.Id, x.Name, x.Id.ToString()));
                ret.SiteId = chosen;
            }
            return ret;
        }

        public override async Task<IISOptions> Default(Target target)
        {
            var siteId = await FtpSite.GetValue();
            siteId ??= await InstallationSite.Required(!target.IIS).GetValue();
            var ret = new IISOptions()
            {
                NewBindingPort = await NewBindingPort.GetValue(),
                NewBindingIp = await NewBindingIp.GetValue(),
                SiteId = siteId
            };
            return ret;
        }
    }
}
