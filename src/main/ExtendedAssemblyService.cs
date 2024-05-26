namespace PKISharp.WACS.Services
{
    public class ExtendedAssemblyService : AssemblyService
    {
        public ExtendedAssemblyService(ILogService logger) : base(logger)
        {
            _allTypes.AddRange(new[] { new TypeDescriptor(typeof(Plugins.ValidationPlugins.Http.Ftp)) });
        }
    }
}