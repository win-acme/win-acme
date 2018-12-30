namespace PKISharp.WACS.Clients.IIS
{
    public class BindingOptions
    {
        public SSLFlags Flags { get; }
        public int Port { get; }
        public string IP { get; }
        public byte[] Thumbprint { get; }
        public string Store { get; }
        public string Host { get; }
        public long? SiteId { get; }

        public string Binding
        {
            get
            {
                return $"{IP}:{Port}:{Host}";
            }
        }

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

        public BindingOptions WithFlags(SSLFlags flags)
        {
            return new BindingOptions(flags, Port, IP, Thumbprint, Store, Host, SiteId);
        }
        public BindingOptions WithPort(int port)
        {
            return new BindingOptions(Flags, port, IP, Thumbprint, Store, Host, SiteId);
        }
        public BindingOptions WithIP(string ip)
        {
            return new BindingOptions(Flags, Port, ip, Thumbprint, Store, Host, SiteId);
        }
        public BindingOptions WithThumbprint(byte[] thumbprint)
        {
            return new BindingOptions(Flags, Port, IP, thumbprint, Store, Host, SiteId);
        }
        public BindingOptions WithStore(string store)
        {
            return new BindingOptions(Flags, Port, IP, Thumbprint, store, Host, SiteId);
        }
        public BindingOptions WithHost(string hostName)
        {
            return new BindingOptions(Flags, Port, IP, Thumbprint, Store, hostName, SiteId);
        }
        public BindingOptions WithSiteId(long? siteId)
        {
            return new BindingOptions(Flags, Port, IP, Thumbprint, Store, Host, siteId);
        }
    }
}