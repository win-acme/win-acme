using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Configuration
{
    /// <summary>
    /// Default ArgumentsProvider that is brought to life by the 
    /// PluginService for each implementation of IArgumentsStandalone
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StandaloneArgumentsProvider<T> : BaseArgumentsProvider<T> where T : class, IArgumentsStandalone, new()
    {
        /// <summary>
        /// Construct an emtpy instance of T to able to use its properties
        /// </summary>
        private T DefaultInstance
        {
            get
            {
                if (_defaultInstance == null)
                {
                    var type = typeof(T);
                    var constructor = type.GetConstructor(Array.Empty<Type>());
                    if (constructor == null)
                    {
                        throw new InvalidOperationException();
                    }
                    _defaultInstance = (T)constructor.Invoke(null);
                }
                return _defaultInstance;
            }
        }
        private T? _defaultInstance;

        public override string Name => DefaultInstance.Name;
        public override string Group => DefaultInstance.Group;
        public override bool Default => DefaultInstance.Default;
        public override string? Condition => DefaultInstance.Condition;
    }
}
