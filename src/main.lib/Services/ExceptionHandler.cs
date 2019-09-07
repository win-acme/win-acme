using Autofac.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Services
{
    internal class ExceptionHandler
    {
        private readonly ILogService _log;

        public ExceptionHandler(ILogService log) => _log = log;

        /// <summary>
        /// Handle exceptions by logging them and setting negative exit code
        /// </summary>
        /// <param name="innerex"></param>
        public string HandleException(Exception original = null, string message = null)
        {
            var outMessage = message;
            var exceptionStack = new List<Exception>();
            if (original != null)
            {
                exceptionStack.Add(original);
                while (original.InnerException != null)
                {
                    original = original.InnerException;
                    exceptionStack.Add(original);
                }
                var innerMost = exceptionStack.Count() - 1;
                for (var i = innerMost; i >= 0; i--)
                {
                    var currentException = exceptionStack[i];
                    if (i == innerMost)
                    {
                        outMessage = currentException.Message;
                        // InnerMost exception is logged with Error priority
                        if (!string.IsNullOrEmpty(message))
                        {
                            _log.Error($"({{type}}) {message}: {{message}}", currentException.GetType().Name, currentException.Message);
                        }
                        else
                        {
                            _log.Error("({type}): {message}", currentException.GetType().Name, currentException.Message);
                        }
                        _log.Debug("Exception details: {@ex}", currentException);
                        Environment.ExitCode = currentException.HResult;
                    }
                    else if (
                        !(currentException is DependencyResolutionException) &&
                        !(currentException is AggregateException))
                    {
                        // Outer exceptions up to the point of Autofac logged with error priority
                        _log.Error("Wrapped in {type}: {message}", currentException.GetType().Name, currentException.Message);
                    }
                    else
                    {
                        // Autofac and Async exceptions only logged in debug/verbose mode
                        _log.Debug("Wrapped in {type}: {message}", currentException.GetType().Name, currentException.Message);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(message))
            {
                _log.Error(message);
                Environment.ExitCode = -1;
            }
            return outMessage;
        }
    }
}
