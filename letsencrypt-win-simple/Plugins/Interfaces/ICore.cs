using System;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IHasName
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Check if name matches
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Match(string name);

        /// <summary>
        /// Human-understandable description
        /// </summary>
        string Description { get; }
    }

    public interface IHasType
    {
        /// <summary>
        /// Which type is used as instance
        /// </summary>
        Type Instance { get; }
    }

    public interface INull {}
}
