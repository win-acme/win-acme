using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class TargetPluginOptions : PluginOptions
    {
        /// <summary>
        /// Pass default FriendlyName from the TargetPluginOptionsFactory 
        /// to the main program, so that can be used for the ScheduledRenewal
        /// (pending implicit or explicit user approval)
        /// </summary>
        [JsonIgnore]
        public string FriendlyNameSuggestion { get; set; }
    }

    public abstract class TargetPluginOptions<T> : TargetPluginOptions where T : ITargetPlugin
    {
        public override abstract string Name { get; }
        public override abstract string Description { get; }

        public override void Show(IInputService input)
        {
            input.Show("Target");
            input.Show("Plugin", $"{Name} - ({Description})", level: 1);
        }

        public override Type Instance => typeof(T);
    }
}
