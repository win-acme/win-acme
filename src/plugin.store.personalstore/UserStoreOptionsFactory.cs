using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class UserStoreOptionsFactory : PluginOptionsFactory<UserStoreOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public UserStoreOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        public override async Task<UserStoreOptions?> Aquire(IInputService inputService, RunLevel runLevel) => 
            await Default();

        public override async Task<UserStoreOptions?> Default()
        {
            return new UserStoreOptions
            {
                KeepExisting = await _arguments.
                    GetBool<UserArguments>(x => x.KeepExisting).
                    WithDefault(false).
                    DefaultAsNull().
                    GetValue()
            };
        }
    }
}
