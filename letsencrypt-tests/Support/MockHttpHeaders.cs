using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace letsencrypt_tests.Support
{
    public class MockHttpHeaders : NameValueCollection
    {
        public MockHttpHeaders() : base() { }

        public MockHttpHeaders(NameValueCollection collection) : base()
        {
            this.Add(collection);
            AddMissing("Timestamp", DateTime.Now.ToString());
            AddMissing("Content-Type", "text/html");
            AddMissing("Cache-Control", "no-cache, no-store, must-revalidate");
            AddMissing("Pragma", "no-cache");
            AddMissing("Expires", "0");
        }

        public void AddMissing(string key, string value)
        {
            if (!AllKeys.Contains(key))
            {
                Add(key, value);
            }
        }
    }
}
