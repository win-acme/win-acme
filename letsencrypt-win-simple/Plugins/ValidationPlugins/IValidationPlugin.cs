using ACMESharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    public interface IValidationPlugin : IHasName
    {
        string ChallengeType { get; }
        Action<AuthorizationState> PrepareChallenge(Options options, Target target, AuthorizeChallenge challenge);
    }
}
