using MorseCode.ITask;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    public class PluginOptionsFactory<TOptions> :
        IPluginOptionsFactory<TOptions>
        where TOptions : PluginOptions, new()
    {
        public virtual int Order => 0;
        public virtual Task<TOptions?> Default() => Task.FromResult<TOptions?>(new TOptions());
        public virtual Task<TOptions?> Aquire(IInputService inputService, RunLevel runLevel) => Default();
        async ITask<TOptions?> IPluginOptionsFactory<TOptions>.Aquire(IInputService inputService, RunLevel runLevel) => await Aquire(inputService, runLevel);
        async ITask<TOptions?> IPluginOptionsFactory<TOptions>.Default() => await Default();
    }
}
