namespace LetsEncrypt.ACME.Simple.Plugins
{
    public interface IHasName
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Human-understandable description
        /// </summary>
        string Description { get; }
    }

    public interface IIsNull {}
}
