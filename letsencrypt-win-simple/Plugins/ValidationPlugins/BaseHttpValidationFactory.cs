using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal abstract class BaseHttpValidationFactory<T> : BaseValidationPluginFactory<T> where T : IValidationPlugin
    {
        public BaseHttpValidationFactory(ILogService log, string name, string description = null) : base(log, name, description) { }

        /// <summary>
        /// Check or get information need for validation (interactive)
        /// </summary>
        /// <param name="target"></param>
        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            // Manual
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                do
                {
                    target.WebRootPath = optionsService.TryGetOption(optionsService.Options.WebRoot, inputService, WebrootHint(target.IIS == true));
                }
                while (!ValidateWebroot(target));
            }

            if (target.IIS == null)
            {
                target.IIS = optionsService.Options.ManualTargetIsIIS;
                if (target.IIS == false)
                {
                    target.IIS = inputService.PromptYesNo("Copy default web.config before validation?");
                }
            }
        }

        /// <summary>
        /// Check information need for validation (unattended)
        /// </summary>
        /// <param name="target"></param>
        public override void Default(Target target, IOptionsService optionsService)
        {
            if (target.IIS == null)
            {
                target.IIS = optionsService.Options.ManualTargetIsIIS;
            }
            if (string.IsNullOrEmpty(target.WebRootPath))
            { 
                target.WebRootPath = optionsService.Options.WebRoot;
                if (!ValidateWebroot(target))
                {
                    throw new ArgumentException($"Invalid webroot {target.WebRootPath}: {WebrootHint(false)[0]}");
                }
            }
        }

        /// <summary>
        /// Check if the webroot makes sense
        /// </summary>
        /// <returns></returns>
        public virtual bool ValidateWebroot(Target target)
        {
            return true;
        }

        /// <summary>
        /// Hint to show about what the webroot should look like
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