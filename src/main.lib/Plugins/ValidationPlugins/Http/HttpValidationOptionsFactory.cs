using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal abstract class HttpValidationOptionsFactory<TOptions> : 
        PluginOptionsFactory<TOptions>
        where TOptions : HttpValidationOptions, new()
    {
        protected readonly ArgumentsInputService _arguments;
        protected readonly Target _target;

        public HttpValidationOptionsFactory(ArgumentsInputService arguments, Target target) 
        {
            _arguments = arguments;
            _target = target;
        }

        private ArgumentResult<string?> Path(bool allowEmpty)
        {
            var pathArg = _arguments.
                GetString<HttpValidationArguments>(x => x.WebRoot).
                Validate(p => Task.FromResult(PathIsValid(p!)), $"invalid path");
            if (!allowEmpty)
            {
                pathArg = pathArg.Required();
            }
            return pathArg;
        }

        private ArgumentResult<bool?> CopyWebConfig =>
            _arguments.
                GetBool<HttpValidationArguments>(x => x.ManualTargetIsIIS).
                DefaultAsNull().
                WithDefault(false);

        /// <summary>
        /// Get webroot path manually
        /// </summary>
        public async Task<HttpValidationOptions?> BaseAquire(IInputService input)
        {
            var allowEmpty = AllowEmtpy();
            var webRootHint = WebrootHint(allowEmpty);
            var pathGetter = Path(allowEmpty);
            pathGetter = webRootHint.Length > 1
                ? pathGetter.Interactive(input, webRootHint[0], string.Join('\n', webRootHint[1..]))
                : pathGetter.Interactive(input, webRootHint[0]);
            return new TOptions
            {
                Path = await pathGetter.GetValue(),
                CopyWebConfig = _target.IIS || await CopyWebConfig.Interactive(input, "Copy default web.config before validation?").GetValue() == true
            };
        }

        /// <summary>
        /// Basic parameters shared by http validation plugins
        /// </summary>
        public async Task<HttpValidationOptions> BaseDefault()
        {
            var allowEmpty = AllowEmtpy();
            return new TOptions
            {
                Path = await Path(allowEmpty).GetValue(),
                CopyWebConfig = _target.IIS || await CopyWebConfig.GetValue() == true
            };
        }

        /// <summary>
        /// By default we don't allow emtpy paths, but FileSystem 
        /// makes an exception because it can read from IIS
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual bool AllowEmtpy() => false;

        /// <summary>
        /// Check if the webroot makes sense
        /// </summary>
        /// <returns></returns>
        public virtual bool PathIsValid(string path) => false;

        /// <summary>
        /// Hint to show to the user what the webroot should look like
        /// </summary>
        /// <returns></returns>
        public virtual string[] WebrootHint(bool allowEmpty)
        {
            var ret = new List<string> { "Path" };
            if (allowEmpty)
            {
                ret.Add("Leave empty to automatically read the path from IIS");
            }
            return ret.ToArray();
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(TOptions options)
        {
            yield return (CopyWebConfig.Meta, options.CopyWebConfig);
            yield return (Path(false).Meta, options.Path);
        }
    }

}