using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS
{
    internal class Unattended
    {
        private readonly IInputService _input;
        private readonly ILogService _log;
        private readonly IRenewalStore _renewalStore;
        private readonly MainArguments _args;
        private readonly DueDateStaticService _dueDate;
        private readonly IRenewalRevoker _renewalRevoker;
        private readonly AcmeClientManager _clientManager;
        private readonly AccountArguments _accountArguments;

        public Unattended(  
            MainArguments args,
            IRenewalStore renewalStore, 
            IInputService input, 
            ILogService log,
            DueDateStaticService dueDate,
            IRenewalRevoker renewalRevoker,
            AccountArguments accountArguments,
            AcmeClientManager clientManager)
        {
            _renewalStore = renewalStore;
            _args = args;
            _input = input;
            _log = log;
            _dueDate = dueDate;
            _renewalRevoker = renewalRevoker;
            _clientManager = clientManager;
            _accountArguments = accountArguments;
        }

        /// <summary>
        /// For command line --list
        /// </summary>
        /// <returns></returns>
        internal async Task List()
        {
            await _input.WritePagedList(
                 _renewalStore.Renewals.Select(x => Choice.Create<Renewal?>(x,
                    description: x.ToString(_dueDate, _input),
                    color: x.History.Last().Success == true ?
                            _dueDate.IsDue(x) ?
                                ConsoleColor.DarkYellow :
                                ConsoleColor.Green :
                            ConsoleColor.Red)));
        }

        /// <summary>
        /// Cancel certificate from the command line
        /// </summary>
        /// <returns></returns>
        internal async Task Cancel()
        {
            var targets = FilterRenewalsByCommandLine("cancel");
            await _renewalRevoker.CancelRenewals(targets);
        }

        /// <summary>
        /// Revoke certifcate from the command line
        /// </summary>
        /// <returns></returns>
        internal async Task Revoke()
        {
            _log.Warning($"Certificates should only be revoked in case of a (suspected) security breach. Cancel the renewal if you simply don't need the certificate anymore.");
            var renewals = FilterRenewalsByCommandLine("revoke");
            await _renewalRevoker.RevokeCertificates(renewals);
        }

        /// <summary>
        /// Register new ACME account from the command line
        /// </summary>
        /// <returns></returns>
        internal async Task Register() => await _clientManager.GetClient(_accountArguments.Account);

        /// <summary>
        /// Filters for unattended mode
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private IEnumerable<Renewal> FilterRenewalsByCommandLine(string command)
        {
            if (_args.HasFilter)
            {
                var targets = _renewalStore.FindByArguments(
                    _args.Id,
                    _args.FriendlyName);
                if (!targets.Any())
                {
                    _log.Error("No renewals matched.");
                }
                return targets;
            }
            else
            {
                _log.Error($"Specify which renewal to {command} using the parameter --id or --friendlyname.");
            }
            return new List<Renewal>();
        }
    }
}