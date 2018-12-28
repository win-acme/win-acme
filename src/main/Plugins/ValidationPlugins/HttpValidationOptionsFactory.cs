using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
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
        protected readonly IISClient _iisClient;

        public HttpValidationOptionsFactory(ILogService log, IISClient iisClient) : base(log)
        {
            _iisClient = iisClient;
        }

        /// <summary>
        /// Get webroot path manually
        /// </summary>
        public HttpValidationOptions<TPlugin> BaseAquire(Target target, IOptionsService options, IInputService input, RunLevel runLevel)
        {
            string path = null;
            var allowEmtpy = target.IIS;
            if (!allowEmtpy && string.IsNullOrEmpty(path))
            {
                path = options.TryGetOption(null, input, WebrootHint(allowEmtpy));
            }
            while ((!string.IsNullOrEmpty(path) && !PathIsValid(path)) || (!allowEmtpy && string.IsNullOrEmpty(path)))
            {
                path = options.TryGetOption(null, input, WebrootHint(allowEmtpy));
            }
            return new TOptions {
                Path = target.IIS ? null : path,
                CopyWebConfig = target.IIS || input.PromptYesNo("Copy default web.config before validation?")
            };
        }

        /// <summary>
        /// Get webroot automatically
        /// </summary>
        public HttpValidationOptions<TPlugin> BaseDefault(Target target, IOptionsService options)
        {
            string path = null;
            var allowEmpty = target.IIS;
            if (string.IsNullOrEmpty(path) && !allowEmpty)
            {
                path = options.TryGetRequiredOption(nameof(options.Options.WebRoot), options.Options.WebRoot);
            }
            if  (!string.IsNullOrEmpty(path) && !PathIsValid(path))
            {
                throw new ArgumentException($"Invalid webroot {path}: {WebrootHint(false)[0]}");
            }
            return new TOptions
            {
                Path = path,
                CopyWebConfig = target.IIS || options.Options.ManualTargetIsIIS
            };
        }

        /// <summary>
        /// Check if the webroot makes sense
        /// </summary>
        /// <returns></returns>
        public virtual bool PathIsValid(string path)
        {
            return true;
        }

        /// <summary>
        /// Hint to show to the user what the webroot should look like
        /// </summary>
        /// <returns></returns>
        public virtual string[] WebrootHint(bool allowEmpty)
        {
            var ret = new List<string> { "Enter the path to the web root of the site that will handle http authentication" };
            if (allowEmpty)
            {
                ret.Add("Leave this input emtpy to use the default path for the chosen target");
            }
            return ret.ToArray();
        }
    }

}