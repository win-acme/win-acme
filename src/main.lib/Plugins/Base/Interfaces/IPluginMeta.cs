using System;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Base class for the attribute is used to find it easily
    /// </summary>
    public interface IPluginMeta
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public bool Hidden { get; }
        public string ChallengeType { get; }
        public Type Capability { get; }
        public Type Options { get; }
        public Type OptionsFactory { get; }
        public Type OptionsJson { get; }
    }

}
