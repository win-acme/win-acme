using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Clients.IIS
{
    public enum IISSiteType
    {
        Web,
        Ftp,
        Unknown
    }

    public interface IIISClient
    {
        void Refresh();
        IEnumerable<IIISSite> Sites { get; }
        IIISSite GetSite(long id, IISSiteType? type = null);
        bool HasFtpSites { get; }
        bool HasWebSites { get; }
        Version Version { get; }
        void UpdateHttpSite(IEnumerable<Identifier> identifiers, BindingOptions bindingOptions, byte[]? oldCertificate = null, IEnumerable<Identifier>? allIdentifiers = null);
        void UpdateFtpSite(long? id, string? store, ICertificateInfo newCertificate, ICertificateInfo? oldCertificate);
    }

    public interface IIISClient<TSite, TBinding> : IIISClient
        where TSite : IIISSite<TBinding>
        where TBinding : IIISBinding
    {
        IIISBinding AddBinding(TSite site, BindingOptions bindingOptions);
        void UpdateBinding(TSite site, TBinding binding, BindingOptions bindingOptions);
        new IEnumerable<TSite> Sites { get; }
        new TSite GetSite(long id, IISSiteType? type);

    }

    public interface IIISSite
    {
        long Id { get; }
        IISSiteType Type { get; }
        string Name { get; }
        string Path { get; }
        IEnumerable<IIISBinding> Bindings { get; }
    }

    public interface IIISSite<TBinding> : IIISSite
        where TBinding : IIISBinding
    {
        new IEnumerable<TBinding> Bindings { get; }
    }

    public interface IIISBinding
    {
        string Host { get; }
        string Protocol { get; }
        bool Secure { get; }
        byte[]? CertificateHash { get; }
        string CertificateStoreName { get; }
        string BindingInformation { get; }
        string? IP { get; }
        SSLFlags SSLFlags { get; }
        int Port { get; }
    }
}