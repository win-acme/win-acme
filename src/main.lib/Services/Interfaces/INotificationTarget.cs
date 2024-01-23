using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services.Interfaces
{
    interface INotificationTarget
    {
        internal Task SendCreated(Renewal renewal, IEnumerable<MemoryEntry> log);
        internal Task SendSuccess(Renewal renewal, IEnumerable<MemoryEntry> log);
        internal Task SendFailure(Renewal renewal, IEnumerable<MemoryEntry> log, IEnumerable<string> errors);
        internal Task SendTest();
    }
}
