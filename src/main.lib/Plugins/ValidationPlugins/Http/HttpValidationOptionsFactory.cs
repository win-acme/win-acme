using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal abstract class HttpValidationOptionsFactory<TPlugin, TOptions> :
        ValidationPluginOptionsFactory<TPlugin, TOptions>
        where TPlugin : IValidationPlugin
        where TOptions : HttpValidationOptions<TPlugin>, new()
    {
        protected readonly ArgumentsInputService _arguments;

        public HttpValidationOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<string?> GetPath(bool allowEmpty)
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

        private ArgumentResult<bool?> GetCopyWebConfig()
        {
            var pathArg = _arguments.
                GetBool<HttpValidationArguments>(x => x.ManualTargetIsIIS).
                DefaultAsNull().
                WithDefault(false);
            return pathArg;
        }

        /// <summary>
        /// Get webroot path manually
        /// </summary>
        public async Task<HttpValidationOptions<TPlugin>> BaseAquire(Target target, IInputService input)
        {
            var allowEmpty = AllowEmtpy(target);
            return new TOptions
            {
                Path = await GetPath(allowEmpty).Interactive(input, WebrootHint(allowEmpty)[0], string.Join('\n', WebrootHint(allowEmpty)[1..])).GetValue(),
                CopyWebConfig = target.IIS || await GetCopyWebConfig().Interactive(input, "Copy default web.config before validation?").GetValue() == true
            };
        }

        /// <summary>
        /// Basic parameters shared by http validation plugins
        /// </summary>
        public async Task<HttpValidationOptions<TPlugin>> BaseDefault(Target target)
        {
            var allowEmpty = AllowEmtpy(target);
            return new TOptions
            {
                Path = await GetPath(allowEmpty).GetValue(),
                CopyWebConfig = target.IIS || await GetCopyWebConfig().GetValue() == true
            };
        }

        /// <summary>
        /// By default we don't allow emtpy paths, but FileSystem 
        /// makes an exception because it can read from IIS
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual bool AllowEmtpy(Target target) => false;

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
            var ret = new List<string> { "Path to the root of the site that will handle authentication" };
            if (allowEmpty)
            {
                ret.Add("Leave empty to automatically read the path from IIS");
            }
            return ret.ToArray();
        }
    }

}