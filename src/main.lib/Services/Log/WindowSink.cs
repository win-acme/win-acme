using NStack;
using PKISharp.WACS.Services;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

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
            var rendered = 0;
            var render = (string? s) =>
            {
                if (string.IsNullOrEmpty(s))
                {
                    return;
                }

                if (rendered >= width)
                {
                    return;
                }

                rendered += RenderUstr(ustring.Make(s), width - rendered);
            };
            int RenderUstr(ustring ustr, int max)
            {
                int byteLen = ustr.Length;
                int used = 0;
                for (int i = 0; i < byteLen;)
                {
                    (var rune, var size) = Utf8.DecodeRune(ustr, i, i - byteLen);
                    var count = Rune.ColumnWidth(rune);
                    //if (count > 1)
                    //{
                    //    driver.AddStr("x");
                    //    used += 1;
                    //    i += size;
                    //    continue;
                    //}
                    if (used + count > max)
                    {
                        break;
                    }
                    driver.AddRune(rune);
                    used += count;
                    i += size;
                }
                return used;
            }
            var renderLabel = (string label, Attribute colors) =>
            {
                driver.SetAttribute(colors);
                render($"{label}");
                driver.SetAttribute(Colors.TopLevel.Normal);
                render("> ");
            };
            switch (evt.Level)
            {
                case LogEventLevel.Verbose:
                    renderLabel("VRB", new Attribute(Color.Black, Color.DarkGray));
                    break;
                case LogEventLevel.Debug:
                    renderLabel("DBG", new Attribute(Color.White, Color.DarkGray));
                    break;
                case LogEventLevel.Information:
                    renderLabel("INF", new Attribute(Color.BrightGreen, Color.Green));
                    break;
                case LogEventLevel.Warning:
                    renderLabel("WRN", new Attribute(Color.BrightYellow, Color.Cyan));
                    break;
                case LogEventLevel.Error:
                    renderLabel("ERR", new Attribute(Color.BrightRed, Color.Red));
                    break;
            }
            foreach (var token in evt.MessageTemplate.Tokens)
            {
                if (token is PropertyToken pt)
                {
                    if (evt.Properties.TryGetValue(pt.PropertyName, out var propertyValue))
                    {
                        driver.SetAttribute(Colors.TopLevel.Normal);
                        render(propertyValue.ToString().Trim('"'));
                        driver.SetAttribute(Colors.TopLevel.Disabled);
                    }
                }
                else
                {
                    driver.SetAttribute(Colors.TopLevel.Disabled);
                    render(token.ToString());
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
