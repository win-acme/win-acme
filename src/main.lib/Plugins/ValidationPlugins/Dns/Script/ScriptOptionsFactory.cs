using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class ScriptOptionsFactory : ValidationPluginOptionsFactory<Script, ScriptOptions>
    {
        private readonly ILogService _log;
        private readonly ArgumentsInputService _arguments;

        public override bool Match(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "dnsscript" => true,
                _ => base.Match(name),
            };
        }

        public ScriptOptionsFactory(ILogService log, ArgumentsInputService arguments) : base(Constants.Dns01ChallengeType)
        {
            _log = log;
            _arguments = arguments;
        }

        private ArgumentResult<string?> CommonScript => _arguments.
            GetString<ScriptArguments>(x => x.DnsScript);

        private ArgumentResult<string?> CreateScript => _arguments.
            GetString<ScriptArguments>(x => x.DnsCreateScript).
            Validate(x => Task.FromResult(x.ValidFile(_log)), "invalid file");

        private ArgumentResult<string?> CreateScriptArguments => _arguments.
            GetString<ScriptArguments>(x => x.DnsCreateScriptArguments).
            WithDefault(Script.DefaultCreateArguments).
            DefaultAsNull();

        private ArgumentResult<string?> DeleteScript => _arguments.
            GetString<ScriptArguments>(x => x.DnsDeleteScript).
            Validate(x => Task.FromResult(x.ValidFile(_log)), "invalid file");

        private ArgumentResult<string?> DeleteScriptArguments => _arguments.
            GetString<ScriptArguments>(x => x.DnsDeleteScriptArguments).            
            WithDefault(Script.DefaultDeleteArguments).
            DefaultAsNull();

        public override async Task<ScriptOptions?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var ret = new ScriptOptions();
            var createScript = await CreateScript.Interactive(input).GetValue();
            string? deleteScript = null;
            var chosen = await input.ChooseFromMenu(
                "How to delete records after validation",
                new List<Choice<Func<Task>>>()
                {
                    Choice.Create<Func<Task>>(() => {
                        deleteScript = createScript;
                        return Task.CompletedTask;
                    }, "Using the same script"),
                    Choice.Create<Func<Task>>(async () => 
                        deleteScript = await DeleteScript.Interactive(input).Required().GetValue()
                    , "Using a different script"),
                    Choice.Create<Func<Task>>(() => Task.CompletedTask, "Do not delete")
                });
            await chosen.Invoke();

            ProcessScripts(ret, null, createScript, deleteScript);

            input.CreateSpace();
            input.Show("{Identifier}", "Domain that's being validated");
            input.Show("{RecordName}", "Full TXT record name");
            input.Show("{Token}", "Expected value in the TXT record");
            input.CreateSpace();
            ret.CreateScriptArguments = await CreateScriptArguments.Interactive(input).GetValue();
            if (!string.IsNullOrWhiteSpace(ret.DeleteScript) || !string.IsNullOrWhiteSpace(ret.Script))
            {
                ret.DeleteScriptArguments = await DeleteScriptArguments.Interactive(input).GetValue();
            }
            return ret;
        }

        public override async Task<ScriptOptions?> Default(Target target)
        {
            var ret = new ScriptOptions();
            var commonScript = await CommonScript.GetValue();
            var createScript = await CreateScript.GetValue();
            var deleteScript = await DeleteScript.GetValue();
            ProcessScripts(ret, commonScript, createScript, deleteScript);
            ret.DeleteScriptArguments = await DeleteScriptArguments.GetValue();
            ret.CreateScriptArguments = await CreateScriptArguments.GetValue();
            return ret;
        }

        /// <summary>
        /// Choose the right script to run
        /// </summary>
        /// <param name="options"></param>
        /// <param name="commonInput"></param>
        /// <param name="createInput"></param>
        /// <param name="deleteInput"></param>
        private void ProcessScripts(ScriptOptions options, string? commonInput, string? createInput, string? deleteInput)
        {
            if (!string.IsNullOrWhiteSpace(commonInput))
            {
                if (!string.IsNullOrWhiteSpace(createInput))
                {
                    _log.Warning($"Ignoring --dnscreatescript because --dnsscript was provided");
                }
                if (!string.IsNullOrWhiteSpace(deleteInput))
                {
                    _log.Warning("Ignoring --dnsdeletescript because --dnsscript was provided");
                }
            }
            if (string.IsNullOrWhiteSpace(commonInput) &&
                string.Equals(createInput, deleteInput, StringComparison.CurrentCultureIgnoreCase))
            {
                commonInput = createInput;
            }
            if (!string.IsNullOrWhiteSpace(commonInput))
            {
                options.Script = commonInput;
            }
            else
            {
                options.CreateScript = string.IsNullOrWhiteSpace(createInput) ? null : createInput;
                options.DeleteScript = string.IsNullOrWhiteSpace(deleteInput) ? null : deleteInput;
            }
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }

}
