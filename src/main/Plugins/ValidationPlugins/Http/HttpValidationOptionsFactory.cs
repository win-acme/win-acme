using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal abstract class HttpValidationOptionsFactory<TPlugin, TOptions> : 
        ValidationPluginOptionsFactory<TPlugin, TOptions>
        where TPlugin : IValidationPlugin
        where TOptions : HttpValidationOptions<TPlugin>, new()
    {
        public HttpValidationOptionsFactory(ILogService log) : base(log) { }

        /// <summary>
        /// Get webroot path manually
        /// </summary>
        public HttpValidationOptions<TPlugin> BaseAquire(Target target, IArgumentsService options, IInputService input, RunLevel runLevel)
        {
            string path = null;
            var allowEmtpy = AllowEmtpy(target);
            path = options.TryGetArgument(null, input, WebrootHint(allowEmtpy));
            while (
                (!string.IsNullOrEmpty(path) && !PathIsValid(path)) || 
                (string.IsNullOrEmpty(path) && !allowEmtpy))
            {
                path = options.TryGetArgument(null, input, WebrootHint(allowEmtpy));
            }
            return new TOptions {
                Path = path,
                CopyWebConfig = target.IIS || input.PromptYesNo("Copy default web.config before validation?", false)
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
        /// Get webroot automatically
        /// </summary>
        public HttpValidationOptions<TPlugin> BaseDefault(Target target, IArgumentsService options)
        {
            string path = null;
            var allowEmpty = AllowEmtpy(target);
            var args = options.GetArguments<HttpValidationArguments>();
            if (string.IsNullOrEmpty(path) && !allowEmpty)
            {
                path = options.TryGetRequiredArgument(nameof(args.WebRoot), args.WebRoot);
            }
            if  (!string.IsNullOrEmpty(path) && !PathIsValid(path))
            {
                throw new ArgumentException($"Invalid webroot {path}: {WebrootHint(false)[0]}");
            }
            return new TOptions
            {
                Path = path,
                CopyWebConfig = target.IIS || args.ManualTargetIsIIS
            };
        }

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