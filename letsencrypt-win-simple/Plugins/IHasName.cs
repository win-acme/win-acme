using System;

namespace LetsEncrypt.ACME.Simple.Plugins
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

    abstract class BasePluginFactory<T> : IHasName, IHasType
    {
        protected string _name;
        protected string _description;

        public BasePluginFactory(string name, string description)
        {
            _name = name;
            _description = description;
        }

        public virtual bool Match(string name)
        {
            return string.Equals(name, _name, StringComparison.InvariantCultureIgnoreCase);
        }

        string IHasName.Name => _name;
        string IHasName.Description => _description;
        Type IHasType.Instance { get { return typeof(T); } }
    }

    public interface INull {}
}
