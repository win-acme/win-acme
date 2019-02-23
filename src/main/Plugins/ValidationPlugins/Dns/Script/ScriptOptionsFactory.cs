using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class ScriptOptionsFactory : ValidationPluginOptionsFactory<Script, ScriptOptions>
    {
        public ScriptOptionsFactory(ILogService log) : base(log, Constants.Dns01ChallengeType) { }

        public override ScriptOptions Aquire(Target target, IArgumentsService arguments, IInputService input, RunLevel runLevel)
        {
            var args = arguments.GetArguments<ScriptArguments>();
            var ret = new ScriptOptions();
            var createScript = "";
            do
            {
                createScript = arguments.TryGetArgument(args.DnsCreateScript, input, "Path to script that creates DNS records");
            }
            while (!createScript.ValidFile(_log));

            var deleteScript = "";
            input.ChooseFromList(
                "How to delete records after validation",
                new List<Choice<Action>>()
                {
                    Choice.Create<Action>(() => { deleteScript = createScript; }, "Using the same script"),
                    Choice.Create<Action>(() => {
                        do {
                            deleteScript = arguments.TryGetArgument(args.DnsDeleteScript, input, "Path to script that deletes DNS records");
                        }
                        while (!deleteScript.ValidFile(_log));
                    }, "Using a different script"),
                    Choice.Create<Action>(() => { }, "Do not delete")
                }).Invoke();

            ProcessScripts(ret, null, createScript, deleteScript);

            input.Show("{Identifier}", "Domain that's being validated");
            input.Show("{RecordName}", "Full TXT record name");
            input.Show("{Token}", "Expected value in the TXT record");
            var createArgs = arguments.TryGetArgument(args.DnsCreateScriptArguments, input, $"Input parameters for create script, or enter for default \"{Script.DefaultCreateArguments}\"");
            var deleteArgs = "";
            if (!string.IsNullOrWhiteSpace(ret.DeleteScript) || 
                !string.IsNullOrWhiteSpace(ret.Script))
            {
                arguments.TryGetArgument(args.DnsDeleteScriptArguments, input, $"Input parameters for delete script, or enter for default \"{Script.DefaultDeleteArguments}\"");
            }
            ProcessArgs(ret, createArgs, deleteArgs);

            return ret;
        }

        public override ScriptOptions Default(Target target, IArgumentsService arguments)
        {
            var args = arguments.GetArguments<ScriptArguments>();
            var ret = new ScriptOptions();
            ProcessScripts(ret, args.DnsScript, args.DnsCreateScript, args.DnsDeleteScript);
            if (!string.IsNullOrEmpty(ret.Script))
            {
                if (!ret.Script.ValidFile(_log))
                {
                    throw new ArgumentException(nameof(args.DnsCreateScript));
                }
            }
            else
            {
                if (!ret.CreateScript.ValidFile(_log))
                {
                    throw new ArgumentException(nameof(args.DnsCreateScript));
                }
                if (!string.IsNullOrEmpty(ret.DeleteScript))
                {
                    if (!ret.DeleteScript.ValidFile(_log))
                    {
                        throw new ArgumentException(nameof(args.DnsDeleteScript));
                    }
                }
            }
           
            ProcessArgs(ret, args.DnsCreateScriptArguments, args.DnsDeleteScriptArguments);
            return ret;
        }

        /// <summary>
        /// Choose the right script to run
        /// </summary>
        /// <param name="options"></param>
        /// <param name="commonInput"></param>
        /// <param name="createInput"></param>
        /// <param name="deleteInput"></param>
        private void ProcessScripts(ScriptOptions options, string commonInput, string createInput, string deleteInput)
        {
            if (string.IsNullOrWhiteSpace(commonInput) && 
                string.Equals(createInput, deleteInput, StringComparison.CurrentCultureIgnoreCase))
            {
                commonInput = createInput;
            }
            if (!string.IsNullOrWhiteSpace(commonInput))
            {
                options.Script = createInput;
                if (!string.IsNullOrWhiteSpace(createInput) &&
                    !string.Equals(createInput, commonInput, StringComparison.InvariantCultureIgnoreCase))
                {
                    _log.Warning($"Ignoring --dnscreatescript because --dnsscript was provided");
                }
                if (!string.IsNullOrWhiteSpace(deleteInput) &&
                    !string.Equals(deleteInput, commonInput, StringComparison.InvariantCultureIgnoreCase))
                {
                    _log.Warning("Ignoring --dnsdeletescript because --dnsscript was provided");
                }
            }
            else
            {
                options.CreateScript = string.IsNullOrWhiteSpace(createInput) ? null : createInput;
                options.DeleteScript = string.IsNullOrWhiteSpace(deleteInput) ? null : deleteInput;
            }
        }

        private void ProcessArgs(ScriptOptions options, string createInput, string deleteInput)
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

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }

}
