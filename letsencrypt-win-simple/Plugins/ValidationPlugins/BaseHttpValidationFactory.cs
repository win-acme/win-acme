using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;

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
                    target.WebRootPath = optionsService.TryGetOption(optionsService.Options.WebRoot, inputService, WebrootHint());
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
            if (target.TargetPluginName != nameof(IISSites))
            {
                if (string.IsNullOrEmpty(target.WebRootPath))
                {
                    target.WebRootPath = optionsService.TryGetRequiredOption(nameof(optionsService.Options.WebRoot), optionsService.Options.WebRoot);
                }
                if (!ValidateWebroot(target))
                {
                    throw new ArgumentException($"Invalid webroot {target.WebRootPath}: {WebrootHint()[0]}");
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
        public virtual string[] WebrootHint()
        {
            return new[] { "Enter a site path (the web root of the host for http authentication)" };
        }

    }
}