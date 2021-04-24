using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class ScriptOptionsFactory : ValidationPluginOptionsFactory<Script, ScriptOptions>
    {
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public override bool Match(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "dnsscript":
                    return true;
                default:
                    return base.Match(name);
            }
        }

        public ScriptOptionsFactory(ILogService log, IArgumentsService arguments) : base(Constants.Dns01ChallengeType)
        {
            _log = log;
            _arguments = arguments;
        }

        public override async Task<ScriptOptions?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<ScriptArguments>();
            var ret = new ScriptOptions();
            string? createScript = null;
            do
            {
                createScript = args?.DnsCreateScript ?? await input.RequestString("Path to script that creates DNS records");
            }
            while (!createScript.ValidFile(_log));

            string? deleteScript = null;
            var chosen = await input.ChooseFromMenu(
                "How to delete records after validation",
                new List<Choice<Func<Task>>>()
                {
                    Choice.Create<Func<Task>>(() => {
                        deleteScript = createScript;
                        return Task.CompletedTask;
                    }, "Using the same script"),
                    Choice.Create<Func<Task>>(async () => {
                        do {
                            deleteScript = args?.DnsDeleteScript ??
                            await input.RequestString("Path to script that deletes DNS records");
                        }
                        while (!deleteScript.ValidFile(_log));
                    }, "Using a different script"),
                    Choice.Create<Func<Task>>(() => Task.CompletedTask, "Do not delete")
                });
            await chosen.Invoke();

            ProcessScripts(ret, null, createScript, deleteScript);

            input.Show("{Identifier}", "Domain that's being validated");
            input.Show("{RecordName}", "Full TXT record name");
            input.Show("{Token}", "Expected value in the TXT record");
            var createArgs = args?.DnsCreateScriptArguments ?? 
                await input.RequestString($"Input parameters for create script, or enter for default \"{Script.DefaultCreateArguments}\"");
            string? deleteArgs = null;
            if (!string.IsNullOrWhiteSpace(ret.DeleteScript) ||
                !string.IsNullOrWhiteSpace(ret.Script))
            {
                deleteArgs = args?.DnsDeleteScriptArguments ?? 
                    await input.RequestString($"Input parameters for delete script, or enter for default \"{Script.DefaultDeleteArguments}\"");
            }
            ProcessArgs(ret, createArgs, deleteArgs);
            return ret;
        }

        public override async Task<ScriptOptions?> Default(Target target)
        {
            var args = _arguments.GetArguments<ScriptArguments>();
            var ret = new ScriptOptions();
            ProcessScripts(ret, args?.DnsScript, args?.DnsCreateScript, args?.DnsDeleteScript);
            if (!string.IsNullOrEmpty(ret.Script))
            {
                if (!ret.Script.ValidFile(_log))
                {
                    _log.Error($"Invalid argument --{nameof(args.DnsScript).ToLower()}");
                    return null;
                }
            }
            else
            {
                if (!ret.CreateScript.ValidFile(_log))
                {
                    _log.Error($"Invalid argument --{nameof(args.DnsCreateScript).ToLower()}");
                    return null;
                }
                if (!string.IsNullOrEmpty(ret.DeleteScript))
                {
                    if (!ret.DeleteScript.ValidFile(_log))
                    {
                        _log.Error($"Invalid argument --{nameof(args.DnsDeleteScript).ToLower()}");
                        return null;
                    }
                }
            }

            ProcessArgs(ret, args?.DnsCreateScriptArguments, args?.DnsDeleteScriptArguments);
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

        private void ProcessArgs(ScriptOptions options, string? createInput, string? deleteInput)
        {
            if (!string.IsNullOrWhiteSpace(createInput) &&
                createInput != Script.DefaultCreateArguments)
            {
                options.CreateScriptArguments = createInput;
            }
            if (!string.IsNullOrWhiteSpace(options.DeleteScript) ||
                !string.IsNullOrWhiteSpace(options.Script))
            {
                if (!string.IsNullOrWhiteSpace(deleteInput) &&
                    deleteInput != Script.DefaultDeleteArguments)
                {
                    options.DeleteScriptArguments = deleteInput;
                }
            }
        }

        public override bool CanValidate(Target target) => true;
    }

}
