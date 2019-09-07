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

        #pragma warning disable
        // Not used, but must be initialized to create settings.config on clean install
        private readonly ISettingsService _settings;
        #pragma warning enable

        private readonly string _server;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly bool _secure;
        private readonly string _senderName;
        private readonly string _senderAddress;
        private readonly string _receiverAddress;

        public EmailClient(ILogService log, ISettingsService settings)
        {
            _log = log;
            _settings = settings; 
            _server = _settings.SmtpServer;
            _port = _settings.SmtpPort;
            _user = _settings.SmtpUser;
            _password = _settings.SmtpPassword;
            _secure = _settings.SmtpSecure;
            _senderName = _settings.SmtpSenderName;
            if (string.IsNullOrWhiteSpace(_senderName))
            {
                _senderName = _settings.ClientNames[0];
            }
            _senderAddress = _settings.SmtpSenderAddress;
            _receiverAddress = _settings.SmtpReceiverAddress;

            // Criteria for emailing to be enabled at all
            Enabled =
                !string.IsNullOrEmpty(_senderAddress) &&
                !string.IsNullOrEmpty(_server) &&
                !string.IsNullOrEmpty(_receiverAddress);
            _log.Verbose("Sending e-mails {_enabled}", Enabled);
        }

        public bool Enabled { get; internal set; }

        public void Send(string subject, string content, MailPriority priority)
        {
            if (Enabled)
            {
                try
                {
                    _log.Information("Sending e-mail with subject {subject} to {_receiverAddress}", subject, _receiverAddress);
                    var sender = new MailAddress(_senderAddress, _senderName);
                    var receiver = new MailAddress(_receiverAddress);
                    var message = new MailMessage(sender, receiver)
                    {
                        Priority = priority,
                        Subject = subject,
                        IsBodyHtml = true,
                        Body = content + $"<p>Sent by win-acme version {Assembly.GetExecutingAssembly().GetName().Version} from {Environment.MachineName}</p>"
                    };
                    using (var server = new SmtpClient(_server, _port) { EnableSsl = _secure })
                    {
                        if (!string.IsNullOrEmpty(_user))
                        {
                            server.Credentials = new NetworkCredential(_user, _password);
                        }
                        server.Send(message);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Problem sending e-mail");
                }
            }
        }

        internal void Test()
        {
            if (!Enabled)
            {
                _log.Error("Email notifications not enabled. Input an SMTP server, sender and receiver in settings.config to enable this.");
            }
            else
            {
                _log.Information("Sending test message...");
                Send("Test notification",
                    "<p>If you are reading this, it means you will receive notifications about critical errors in the future.</p>",
                    MailPriority.Normal);
                _log.Information("Test message sent!");
            }
        }
    }
}
