using PKISharp.WACS.Services;
using System;
using System.Net;
using System.Net.Mail;
using System.Reflection;

namespace PKISharp.WACS.Clients
{
    class EmailClient
    {
        private readonly ILogService _log;
        private readonly string _server;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly bool _secure;
        private readonly string _senderName;
        private readonly string _senderAddress;
        private readonly string _receiverAddress;
        private readonly bool _enabled = false;

        public EmailClient(ILogService log)
        {
            _log = log;
            // Criteria for emailing to be enabled at all
            _enabled =
                !string.IsNullOrEmpty(_senderAddress) &&
                !string.IsNullOrEmpty(_server) &&
                !string.IsNullOrEmpty(_receiverAddress);
            _log.Verbose("Sending e-mails {_enabled}", _enabled);
        }

        public void Send(string subject, string content)
        {
            if (_enabled)
            {
                try
                {
                    _log.Information("Sending e-mail with subject {subject} to {_receiverAddress}", subject, _receiverAddress);
                    var sender = new MailAddress(_senderAddress, _senderName);
                    var receiver = new MailAddress(_receiverAddress);
                    var message = new MailMessage(sender, receiver)
                    {
                        Subject = subject,
                        Body = content + $"\nSent by win-acme version {Assembly.GetExecutingAssembly().GetName().Version} from {Environment.MachineName}"
                    };
                    var server = new SmtpClient(_server, _port)
                    {
                        EnableSsl = _secure
                    };
                    if (!string.IsNullOrEmpty(_user))
                    {
                        server.Credentials = new NetworkCredential(_user, _password);
                    }
                    server.Send(message);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Problem sending e-mail");
                }
            }
        }
    }
}
