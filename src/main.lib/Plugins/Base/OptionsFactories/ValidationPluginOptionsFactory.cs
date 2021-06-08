using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    public abstract class ValidationPluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        IValidationPluginOptionsFactory
        where TPlugin : IValidationPlugin
        where TOptions : ValidationPluginOptions, new()
    {
        private readonly string _challengeType;
        string IValidationPluginOptionsFactory.ChallengeType => _challengeType;
        public ValidationPluginOptionsFactory(string challengeType = Constants.Http01ChallengeType) => _challengeType = challengeType;

        public abstract Task<TOptions?> Aquire(Target target, IInputService inputService, RunLevel runLevel);
        public abstract Task<TOptions?> Default(Target target);
        async Task<ValidationPluginOptions?> IValidationPluginOptionsFactory.Aquire(Target target, IInputService inputService, RunLevel runLevel) => await Aquire(target, inputService, runLevel);
        async Task<ValidationPluginOptions?> IValidationPluginOptionsFactory.Default(Target target) => await Default(target);

        /// <summary>
        /// By default no plugin can validate wildcards, should be overridden
        /// in the DNS plugins
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual bool CanValidate(Target target) => !target.GetIdentifiers(false).Any(x => x.Value.StartsWith("*."));

    }
}
