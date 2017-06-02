using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;

namespace letsencrypt.Support
{
    public interface IIISServerManager : IDisposable
    {
        void CommitChanges();
        Version GetVersion();
        IEnumerable<IIISSite> Sites { get; }
    }

    public interface IIISSite
    {
        IEnumerable<IIISBinding> Bindings { get; }
        string GetPhysicalPath();
        long Id { get; }
        string Name { get; }

        IIISBinding AddBinding(string bindingInformation, byte[] certificateHash, string certificateStoreName);
        IIISBinding AddBinding(string bindingInformation, string bindingProtocol);
    }

    public interface IIISBinding
    {
        byte[] CertificateHash { set; }
        string CertificateStoreName { set; }
        string Host { get; }
        string Protocol { get; set; }
        string GetEndPoint();
        void SetAttributeValue(string name, object value);
        object GetAttributeValue(string name);
    }
}
