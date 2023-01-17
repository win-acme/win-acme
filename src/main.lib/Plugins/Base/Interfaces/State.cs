namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Structure to return enabled state with matching reason.
    /// </summary>
    public readonly record struct State
    {
        /// <summary>
        /// Enabled state
        /// </summary>
        public static State EnabledState() => new(false, null);

        /// <summary>
        /// Disabled state
        /// </summary>
        /// <returns></returns>
        public static State DisabledState(string reason) => new(true, reason);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="disabled"></param>
        /// <param name="reason"></param>
        private State(bool disabled, string? reason) 
        {
            Disabled = disabled;
            Reason = reason;
        }

        public bool Disabled { get; }
        public string? Reason { get; }
    }
}
