using System.Diagnostics;

namespace PKISharp.WACS.Clients.IIS
{
    /// <summary>
    /// Class to communicate desired binding state to the IISclient
    /// Follows the fluent/immutable pattern
    /// </summary>
    [DebuggerDisplay("Binding {Binding}")]
    public class BindingOptions
    {
        /// <summary>
        /// Desired flags 
        /// </summary>
        public SSLFlags Flags { get; }

        /// <summary>
        /// Port to use when a new binding has to be created
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// IP address to use when a new binding has to be created
        /// </summary>
        public string IP { get; }

        /// <summary>
        /// Certificate thumbprint that should be set for the binding
        /// </summary>
        public byte[] Thumbprint { get; }

        /// <summary>
        /// Certificate store where the certificate can be found
        /// </summary>
        public string Store { get; }

        /// <summary>
        /// Hostname that should be set for the binding
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Optional: SiteId where new binding are supposed to be created
        /// </summary>
        public long? SiteId { get; }

        /// <summary>
        /// Binding string to use in IIS
        /// </summary>
        public string Binding => $"{IP}:{Port}:{Host}";
        public override string ToString() => Binding;

        /// <summary>
        /// Regular constructor
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="port"></param>
        /// <param name="ip"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        /// <param name="hostName"></param>
        /// <param name="siteId"></param>
        public BindingOptions(
            SSLFlags flags = SSLFlags.None,
            int port = IISClient.DefaultBindingPort,
            string ip = IISClient.DefaultBindingIp,
            byte[] thumbprint = null,
            string store = null,
            string hostName = null,
            long? siteId = null)
        {
            Flags = flags;
            Port = port;
            IP = ip;
            Thumbprint = thumbprint;
            Store = store;
            Host = hostName;
            SiteId = siteId;
        }

        public BindingOptions WithFlags(SSLFlags flags) => new BindingOptions(flags, Port, IP, Thumbprint, Store, Host, SiteId);
        public BindingOptions WithPort(int port) => new BindingOptions(Flags, port, IP, Thumbprint, Store, Host, SiteId);
        public BindingOptions WithIP(string ip) => new BindingOptions(Flags, Port, ip, Thumbprint, Store, Host, SiteId);
        public BindingOptions WithThumbprint(byte[] thumbprint) => new BindingOptions(Flags, Port, IP, thumbprint, Store, Host, SiteId);
        public BindingOptions WithStore(string store) => new BindingOptions(Flags, Port, IP, Thumbprint, store, Host, SiteId);
        public BindingOptions WithHost(string hostName) => new BindingOptions(Flags, Port, IP, Thumbprint, Store, hostName, SiteId);
        public BindingOptions WithSiteId(long? siteId) => new BindingOptions(Flags, Port, IP, Thumbprint, Store, Host, siteId);
    }
}