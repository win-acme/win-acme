using ACMESharp.Authorizations;
using System;

namespace PKISharp.WACS
{
    /// <summary>
    /// Execution flags to enable/disable certain functions
    /// for different types of runs
    /// </summary>
    [Flags]
    public enum RunLevel
    {
        Unattended = 1,
        Interactive = 2,
        Simple = 4,
        Advanced = 8,
        Test = 16,
        Import = 32
    }

    public static class Constants
    {
        public const string Dns01ChallengeType = Dns01ChallengeValidationDetails.Dns01ChallengeType;
        public const string Http01ChallengeType = Http01ChallengeValidationDetails.Http01ChallengeType;
    }
   
}