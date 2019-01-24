using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Clients.IIS
{
    public interface IIISClient
    {
        IEnumerable<IIISSite> FtpSites { get; }
        bool HasFtpSites { get; }
        bool HasWebSites { get; }
        Version Version { get; }
        IEnumerable<IIISSite> WebSites { get; }
        
        void AddOrUpdateBindings(IEnumerable<string> identifiers, BindingOptions bindingOptions, byte[] oldThumbprint);
        IIISSite GetFtpSite(long id);
        IIISSite GetWebSite(long id);
        void UpdateFtpSite(long siteId, CertificateInfo newCertificate, CertificateInfo oldCertificate);
    }

    public interface IIISClient<TSite, TBinding> : IIISClient
        where TSite: IIISSite<TBinding>
        where TBinding : IIISBinding
    {
        new IEnumerable<TSite> FtpSites { get; }
        new IEnumerable<TSite> WebSites { get; }
        new TSite GetFtpSite(long id);
        new TSite GetWebSite(long id);
    }

    public interface IIISSite
    {
        long Id { get; }
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
    }
}