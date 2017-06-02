using System;
using Microsoft.Web.Administration;

namespace letsencrypt.Support
{
    internal class IISBindingWrapper : IIISBinding
    {
        private Binding binding;

        public IISBindingWrapper(Binding b)
        {
            this.binding = b;
        }

        public byte[] CertificateHash
        {
            set
            {
                binding.CertificateHash = value;
            }
        }

        public string CertificateStoreName
        {
            set
            {
                binding.CertificateStoreName = value;
            }
        }

        public string Host
        {
            get
            {
                return binding.Host;
            }
        }

        public string Protocol
        {
            get
            {
                return binding.Protocol;
            }
        }

        string IIISBinding.Protocol
        {
            get
            {
                return binding.Protocol;
            }

            set
            {
                binding.Protocol = value;
            }
        }

        public string GetEndPoint()
        {
            return binding.EndPoint.ToString();
        }

        public object GetAttributeValue(string name)
        {
            return binding.GetAttributeValue(name);
        }

        public void SetAttributeValue(string name, object value)
        {
            binding.SetAttributeValue(name, value);
        }
    }
}