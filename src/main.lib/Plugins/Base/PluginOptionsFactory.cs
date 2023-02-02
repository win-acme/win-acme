using MorseCode.ITask;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    public class PluginOptionsFactory<TOptions> :
        IPluginOptionsFactory<TOptions>
        where TOptions : PluginOptions, new()
    {
        public virtual int Order => 0;
        public virtual IEnumerable<string> Describe(TOptions options) => new List<string>();
        public virtual Task<TOptions?> Default() => Task.FromResult<TOptions?>(new TOptions());
        public virtual Task<TOptions?> Aquire(IInputService inputService, RunLevel runLevel) => Default();
        async ITask<TOptions?> IPluginOptionsFactory<TOptions>.Aquire(IInputService inputService, RunLevel runLevel) => await Aquire(inputService, runLevel);
        async ITask<TOptions?> IPluginOptionsFactory<TOptions>.Default() => await Default();
        string IPluginOptionsFactory<TOptions>.Describe(PluginOptions options) => options is TOptions typed ? string.Join(" ", Describe(typed).Where(x => !string.IsNullOrWhiteSpace(x))) : "";

        protected string Escape(string value)
        {
            if (value.Contains(' ') || value.Contains('"'))
            {
                return $"\"{value.Replace("\"", "\\\"")}\"";
            }
            return value;
        }
    }
}
