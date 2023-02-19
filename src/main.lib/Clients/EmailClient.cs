using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    internal class EmailClient
    {
        private readonly ILogService _log;

#pragma warning disable
        // Not used, but must be initialized to create settings.json on clean install
        private readonly ISettingsService _settings;
#pragma warning enable

        private readonly string _server;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly bool _secure;
        private readonly int? _secureMode;
        private readonly string _senderName;
        private readonly string _senderAddress;
        private readonly string _computerName;
        private readonly string _version;
        private readonly IEnumerable<string> _receiverAddresses;

        public EmailClient(
            ILogService log, 
            ISettingsService settings, 
            SecretServiceManager secretService)
        {
            _log = log;
            _settings = settings;
            _server = _settings.Notification.SmtpServer;
            _port = _settings.Notification.SmtpPort;
            _user = _settings.Notification.SmtpUser;
            _password = secretService.EvaluateSecret(_settings.Notification.SmtpPassword);
            _secure = _settings.Notification.SmtpSecure;
            _secureMode = _settings.Notification.SmtpSecureMode;
            _senderName = _settings.Notification.SenderName;
            _computerName = _settings.Notification.ComputerName;
            if (string.IsNullOrEmpty(_computerName)) {
                _computerName = Environment.MachineName;
            }
            _version = VersionService.SoftwareVersion.ToString();

            if (string.IsNullOrWhiteSpace(_senderName))
            {
                _senderName = _settings.Client.ClientName;
            }
            _senderAddress = _settings.Notification.SenderAddress;
            _receiverAddresses = _settings.Notification.ReceiverAddresses ?? new List<string>();

            // Criteria for emailing to be enabled at all
            Enabled =
                !string.IsNullOrEmpty(_senderAddress) &&
                !string.IsNullOrEmpty(_server) &&
                _receiverAddresses.Any();
            _log.Verbose("Sending e-mails {_enabled}", Enabled);
        }

        public bool Enabled { get; internal set; }

        public async Task<bool> Send(string subject, string content, MessagePriority priority)
        {
            if (!Enabled)
            {
                return false;
            }
            using var client = new SmtpClient();
            try
            {
                var options = SecureSocketOptions.None;
                if (_secure)
                {
                    if (_secureMode.HasValue)
                    {
                        switch (_secureMode.Value)
                        {
                            case 1:
                                options = SecureSocketOptions.Auto;
                                break;
                            case 2:
                                options = SecureSocketOptions.SslOnConnect;
                                break;
                            case 3:
                                options = SecureSocketOptions.StartTls;
                                break;
                            case 4:
                                options = SecureSocketOptions.StartTlsWhenAvailable;
                                break;
                        }
                    }
                    else
                    {
                        options = SecureSocketOptions.StartTls;
                    }
                }
                await client.ConnectAsync(_server, _port, options);
                if (!string.IsNullOrEmpty(_user))
                {
                    await client.AuthenticateAsync(new NetworkCredential(_user, _password));
                }
                foreach (var receiverAddress in _receiverAddresses)
                {
                    _log.Information("Sending e-mail with subject {subject} to {_receiverAddress}", subject, receiverAddress);
                    var sender = new MailboxAddress(_senderName, _senderAddress);
                    var receiver = new MailboxAddress("Receiver", receiverAddress);
                    var message = new MimeMessage()
                    {
                        Sender = sender,
                        Priority = priority,
                        Subject = subject
                    };
                    message.Subject = $"{subject} ({_computerName})";
                    message.From.Add(sender);
                    message.To.Add(receiver);
                    var bodyBuilder = new BodyBuilder();
                    bodyBuilder.HtmlBody = content + $"<p>Sent by win-acme version {_version} from {_computerName}</p>";
                    message.Body = bodyBuilder.ToMessageBody();
                    await client.SendAsync(message);
                }                       
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Problem sending e-mail");
                return false;
            }
            return true;
        }

        internal async Task Test()
        {
            if (!Enabled)
            {
                _log.Error("Email notifications not enabled. Configure an SMTP server, sender and receiver in settings.json to enable this.");
            }
            else
            {
                _log.Information("Sending test message...");
                var success = await Send("Test notification",
                    "<p>If you are reading this, it means you will receive notifications about critical errors in the future.</p>",
                    MessagePriority.Normal);
                if (success)
                {
                    _log.Information("Test message sent!");
                }
            }
        }
    }
}
