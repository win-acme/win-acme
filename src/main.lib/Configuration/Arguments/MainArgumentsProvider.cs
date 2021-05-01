namespace PKISharp.WACS.Configuration.Arguments
{
    internal class MainArgumentsProvider : BaseArgumentsProvider<MainArguments>
    {
        public override string Name => "Main";
        public override string Group => "";
        public override string Condition => "";

        protected override bool IsActive(MainArguments current)
        {
            return
                !string.IsNullOrEmpty(current.FriendlyName) ||
                !string.IsNullOrEmpty(current.Installation) ||
                !string.IsNullOrEmpty(current.Store) ||
                !string.IsNullOrEmpty(current.Order) ||
                !string.IsNullOrEmpty(current.Csr) ||
                !string.IsNullOrEmpty(current.Target) ||
                !string.IsNullOrEmpty(current.Validation);
        }
    }
}