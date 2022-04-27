using PKISharp.WACS.DomainObjects;
using System;

namespace PKISharp.WACS.Services
{
    public interface IDueDateService
    {
        /// <summary>
        /// The latest date that this renewal should be processed
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        public DateTime? DueDate(Renewal renewal);

        /// <summary>
        /// Is the renewal currently (over)due? 
        /// This influences the GUI.
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        public bool IsDue(Renewal renewal);

        /// <summary>
        /// Should the renewal run?
        /// This influences the execution.
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        public bool ShouldRun(Renewal renewal);

        /// <summary>
        /// Is the order currently due?
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public bool ShouldRun(Order order);
    }
}
