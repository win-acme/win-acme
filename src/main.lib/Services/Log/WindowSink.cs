using PKISharp.WACS.Services;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using Terminal.Gui;

namespace PKISharp.WACS.Services
{
    class SerilogDataSource : IListDataSource
    {
        private readonly List<LogEvent> _logEvents = new();
        public void Add(LogEvent evt) => _logEvents.Add(evt);
        public int Count => _logEvents.Count;
        public int Length => _logEvents.Count;
        public bool IsMarked(int item) => false;
        public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
        {
            var evt = _logEvents[item];
            foreach (var token in evt.MessageTemplate.Tokens)
            {
                if (token is PropertyToken pt)
                {
                    if (evt.Properties.TryGetValue(pt.PropertyName, out var propertyValue))
                    {
                        driver.SetAttribute(Colors.TopLevel.Normal);
                        driver.AddStr(propertyValue.ToString().Trim('"')); 
                        driver.SetAttribute(Colors.TopLevel.Disabled);
                    } 
                    else
                    {
                        driver.AddStr("$$");
                    }
                } 
                else
                {
                    driver.SetAttribute(Colors.TopLevel.Disabled);
                    driver.AddStr(token.ToString());
                    driver.SetAttribute(Colors.TopLevel.Normal);
                }
            }
        }
        public void SetMark(int item, bool value) {}
        public IList ToList() => _logEvents;
    }
    class TerminalSink : ILogEventSink
    {
        private readonly IFormatProvider? _formatProvider;
        private readonly SerilogDataSource _data;
        private readonly ListView _list;
        
        public TerminalSink(ListView list, IFormatProvider? formatProvider = null)
        {
            _formatProvider = formatProvider;
            _data = new SerilogDataSource();
            _list = list;
            list.Source = _data;
        }

        public void Emit(LogEvent logEvent)
        {
            _data.Add(logEvent);
            _list.MoveEnd();
        }
    }
}

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.Memory() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationTerminalExtensions
    {
        public static LoggerConfiguration Terminal(this LoggerSinkConfiguration loggerConfiguration, ListView target, IFormatProvider? formatProvider = null) => loggerConfiguration.Sink(new TerminalSink(target, formatProvider));
    }
}
