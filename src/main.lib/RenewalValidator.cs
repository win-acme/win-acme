using Autofac;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Protocol = ACMESharp.Protocol.Resources;

namespace PKISharp.WACS
{
    /// <summary>
    /// This part of the code handles answering validation challenges
    /// 
    /// Roughly there are three stages to this, with implementation 
    /// details for three of them left to plugins to handle.
    /// - Prepare: get ready to answer single challenge
    /// - Commit: last call to get ready (useful for bundling 
    ///   costly operations for multiple challenges)
    /// - Submit: talk to the ACME service, letting it know 
    ///   we're ready on our site and awaiting the response
    /// - Cleanup: we're done (for better or worse), 
    ///   delete temporary stuff.
    ///   
    /// We can handle multiple challenges in parallel if/when
    /// the plugin indicates support for this, even if those 
    /// challenges are coming from different orders in a renewal.
    /// </summary>
    internal class RenewalValidator
    {
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ILogService _log;
        private readonly IPluginService _plugin;
        private readonly ISettingsService _settings;
        private readonly IValidationOptionsService _validationOptions;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly AcmeClient _client;

        public RenewalValidator(
            IAutofacBuilder scopeBuilder,
            ISettingsService settings,
            ILogService log,
            IPluginService plugin,
            AcmeClient client,
            IValidationOptionsService validationOptions,
            ExceptionHandler exceptionHandler)
        {
            _scopeBuilder = scopeBuilder;
            _log = log;
            _validationOptions = validationOptions;
            _exceptionHandler = exceptionHandler;
            _settings = settings;
            _client = client; 
            _plugin = plugin;
        }

        /// <summary>
        /// Ensure that validation challenges for or more orders are completed.
        /// This allows us to split the renewal into multiple orders 
        /// (potentially hundreds), yet still use the parallel capabilities of
        /// the plugins so save a lot of runtime.
        /// </summary>
        /// <param name="orderContexts"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal async Task ValidateOrders(IEnumerable<OrderContext> orderContexts, RunLevel runLevel)
        {
            var contextTasks = new List<Task<AuthorizationContext?>>();
            foreach (var orderContext in orderContexts)
            {
                if (orderContext.Order.Details == null)
                {
                    throw new InvalidOperationException();
                }

                // Get authorization details
                var authorizationUris = orderContext.Order.Details.Payload.Authorizations?.ToList() ?? new List<string>();
                var authorizationTasks = authorizationUris.Select(async uri =>
                {
                    var auth = await GetAuthorizationDetails(orderContext, uri);
                    if (auth != null)
                    {
                        return new AuthorizationContext(orderContext, auth, uri);
                    }
                    return null;
                });
                contextTasks.AddRange(authorizationTasks);
            }

            // Run all GetAuthorizationDetails in parallel
            var authorizations = await Task.WhenAll(contextTasks);

            // Stop if any of them has failed
            if (orderContexts.Any(x => x.OrderResult.Success == false) || authorizations == null)
            {
                return;
            }

            // Map contexts to plugins
            var runnable = authorizations.OfType<AuthorizationContext>();
            var mapping = await CreateMapping(runnable);
            if (mapping == null)
            {
                return;
            }

            // Actually run them for each mapped group
            foreach (var group in mapping)
            {
                var options = group.Key;
                var list = group.Value;
                await RunAuthorizations(options, list, runLevel);
            }
        }

        /// <summary>
        /// Execute specific challenges as gathered by the caller.
        /// Multiple validation plugins may be involved in this process,
        /// because the IValidationOptionsService allows users to 
        /// provide specific validation options for a domain, overruling
        /// the options provided at the level of the renewal itself.
        /// </summary>
        /// <param name="authorisationContexts"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task RunAuthorizations(ValidationPluginOptions pluginOptions, List<AuthorizationContext> authorizations, RunLevel runLevel)
        {
            // Execute them per group, where one group means one validation plugin
            var multipleOrders = authorizations.Any(x => x.Order != authorizations.First().Order);
            using var validationScope = _scopeBuilder.PluginBackend<IValidationPlugin, IValidationPluginCapability, ValidationPluginOptions>(authorizations.First().Order.OrderScope, pluginOptions);
            var plugin = validationScope.Resolve<IValidationPlugin>();
            var contexts = authorizations.Select(context =>
            {
                var targetPart = context.Order.Target.Parts.FirstOrDefault(p => p.Identifiers.Any(i => i == Identifier.Parse(context.Authorization).Unicode(true)));
                if (targetPart == null)
                {
                    throw new InvalidOperationException("Authorisation found that doesn't match target");
                }
                var pluginMeta = _plugin.GetPlugin(pluginOptions);
                return new ValidationContextParameters(context, targetPart, pluginOptions, pluginMeta);
            }).ToList();

            // Choose between parallel and serial execution
            if (_settings.Validation.DisableMultiThreading != false || plugin.Parallelism == ParallelOperations.None)
            {
                await SerialValidation(contexts, validationScope, breakOnError: !multipleOrders);
            }
            else
            {
                await ParallelValidation(plugin.Parallelism, validationScope, contexts, runLevel);
            }

            // Deactivate any remaining authorizations that are still pending
            // due to an error. This prevents users from running into the rate 
            // limit of 300 pending authorizations.
            try
            {
                var deactivateTasks = authorizations.
                        Where(a => a.Authorization.Status == AcmeClient.AuthorizationPending).
                        Select(a => { 
                            _log.Information("[{identifier}] Deactivating pending authorization", a.Label);
                            return _client.DeactivateAuthorization(a.Uri);
                        });
                await Task.WhenAll(deactivateTasks);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to deactivate authorizations");
            }
        }

        /// <summary>
        /// Map authorization contexts to plugins that will handle them.
        /// These can be locally defined (i.e. in the renewal, as is 
        /// traditionally the case), or globally definied (i.e. in the 
        /// yet to be implemented IValidationOptionsService). 
        /// </summary>
        /// <param name="authorisationContexts"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<Dictionary<ValidationPluginOptions, List<AuthorizationContext>>?> CreateMapping(IEnumerable<AuthorizationContext> authorisationContexts)
        {
            var ret = new Dictionary<ValidationPluginOptions, List<AuthorizationContext>>();
            var add = (ValidationPluginOptions o, AuthorizationContext a) => {
                if (ret.TryGetValue(o, out var value))
                {
                    value.Add(a);
                }
                else
                {
                    ret.Add(o, new List<AuthorizationContext>() { a });
                }
            };
            foreach (var authorisationContext in authorisationContexts)
            {
                if (authorisationContext.Authorization == default)
                {
                    throw new InvalidOperationException();
                }
                // Global options (from IValidationOptionsService)
                // get priority over "native" or local options, which
                // are specified at the level of the renewal
                var localOptions = authorisationContext.Order.Renewal.ValidationPluginOptions;
                var identifier = Identifier.Parse(authorisationContext.Authorization);
                var globalOptions = await _validationOptions.GetValidationOptions(identifier);
                if (globalOptions != null &&
                    CanValidate(authorisationContext, globalOptions))
                {
                    add(globalOptions, authorisationContext);
                }
                else if ((globalOptions == null || localOptions.Plugin != globalOptions.Plugin) &&
                    CanValidate(authorisationContext, localOptions))
                {
                    add(localOptions, authorisationContext);
                }
                else
                {
                    // Sanity check, but can happen when a plugin is disabled
                    // for example due to requiring admin rights and the program
                    // not running as admin.
                    _log.Error("No plugin found that can challenge for {authorisation}", authorisationContext.Authorization.Identifier?.Value);
                    authorisationContext.Order.OrderResult.AddErrorMessage($"No plugin found that can challenge for {authorisationContext.Authorization.Identifier?.Value}", authorisationContext.Order.Order.Valid != true);
                    return null;
                }
            }
            return ret;
        }

        /// <summary>
        /// Will the selected validation plugin be able to validate the authorisation?
        /// </summary>
        /// <param name="authorization"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private bool CanValidate(AuthorizationContext context, ValidationPluginOptions options)
        {
            if (context.Authorization.Identifier == null)
            {
                throw new Exception("Missing identifier");
            }
            var identifier = Identifier.Parse(context.Authorization.Identifier);
            var pluginFrontend = _scopeBuilder.ValidationFrontend(context.Order.OrderScope, options, identifier);
            var state = pluginFrontend.Capability.State;
            if (state.Disabled)
            {
                _log.Warning("Validation plugin {name} is not available. {disabledReason}", pluginFrontend.Meta.Name, state.Reason);
                return false;
            }
            if (!context.Authorization.Challenges?.Any(x => x.Type == pluginFrontend.Capability.ChallengeType) ?? false)
            {
                _log.Warning("No challenge of type {challengeType} available", pluginFrontend.Capability.ChallengeType);
                return context.Authorization.Status == AcmeClient.AuthorizationValid;
            }
            return true;
        }

        /// <summary>
        /// Handle multiple validations in parallel 
        /// </summary>
        /// <returns></returns>
        private async Task ParallelValidation(ParallelOperations level, ILifetimeScope validationScope, List<ValidationContextParameters> parameters, RunLevel runLevel)
        {
            var contexts = parameters.Select(parameter => new ValidationContext(validationScope, parameter)).ToList();
            var plugin = contexts.First().ValidationPlugin;
            try
            {
                // Prepare for challenge answer
                if (level.HasFlag(ParallelOperations.Prepare))
                {
                    // Parallel
                    _log.Verbose("Handle {n} preparation(s)", contexts.Count);
                    var prepareTasks = contexts.Select(vc => Prepare(vc, runLevel));
                    await Task.WhenAll(prepareTasks);
                }
                else
                {
                    // Serial
                    foreach (var ctx in contexts)
                    {
                        await Prepare(ctx, runLevel);
                    }
                }

                // Filter out failed contexts
                contexts = contexts.Where(x => x.OrderResult.Success != false).ToList();
                if (!contexts.Any())
                {
                    return;
                }

                // Commit
                var commited = await Commit(plugin);
                if (!commited)
                {
                    foreach (var x in contexts)
                    {
                        x.OrderResult.AddErrorMessage("Validation plugin commit stage failed");
                    }
                    return;
                }

                // Submit challenge answer
                var contextsWithChallenges = contexts.Where(x => x.Challenge != null).ToList();
                if (contextsWithChallenges.Any())
                {
                    if (level.HasFlag(ParallelOperations.Answer))
                    {
                        // Parallel
                        _log.Verbose("Handle {n} answers(s)", contextsWithChallenges.Count);
                        var submitTasks = contextsWithChallenges.Select(Submit);
                        await Task.WhenAll(submitTasks);
                    }
                    else
                    {
                        // Serial
                        foreach (var ctx in contextsWithChallenges)
                        {
                            await Submit(ctx);
                        }
                    }
                }
            }
            finally
            {
                // Cleanup
                await Cleanup(plugin);
            }
        }

        /// <summary>
        /// Handle validation in serial order. This is basically a wrapper
        /// around the ParellelValidation function, just sending the parameter
        /// sets through there one by one.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task SerialValidation(IList<ValidationContextParameters> parameters, ILifetimeScope globalScope, bool breakOnError)
        {
            foreach (var parameter in parameters)
            {
                _log.Verbose("Handle authorization {n}/{m}",
                    parameters.IndexOf(parameter) + 1,
                    parameters.Count);
                if (!parameter.OrderContext.OrderResult.Success == false)
                {
                    _log.Verbose("Skip authorization because the order has already failed");
                    continue;
                }

                // For serial mode we *MUST* create a seperate DI scope 
                // for each identifier if the plugin is not capable/aware
                // of any parallel operation, because it might not properly
                // maintain its internal state for multiple uses. 
                var validationScope = globalScope;
                var capability = globalScope.Resolve<IValidationPlugin>();
                if (!capability.Parallelism.HasFlag(ParallelOperations.Reuse))
                {
                    validationScope = _scopeBuilder.PluginBackend<IValidationPlugin, IValidationPluginCapability, ValidationPluginOptions>(parameter.OrderContext.OrderScope, parameter.Options);
                }
                await ParallelValidation(
                    ParallelOperations.None,
                    validationScope, 
                    new List<ValidationContextParameters> { parameter }, 
                    parameter.OrderContext.RunLevel);
                if (breakOnError && parameter.OrderContext.OrderResult.Success == false)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Get authorization details from server
        /// </summary>
        /// <param name="context"></param>
        /// <param name="authorizationUri"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<Protocol.AcmeAuthorization?> GetAuthorizationDetails(OrderContext context, string authorizationUri)
        {
            // Get authorization challenge details from server
            var client = context.OrderScope.Resolve<AcmeClient>();
            Protocol.AcmeAuthorization? authorization;
            try
            {
                authorization = await client.GetAuthorizationDetails(authorizationUri);
            }
            catch
            {
                context.OrderResult.AddErrorMessage($"Unable to get authorization details from {authorizationUri}", context.Order.Valid != true);
                return null;
            }
            return authorization;
        }

        /// <summary>
        /// Parse the challenge and prepare to answer it.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task Prepare(ValidationContext context, RunLevel runLevel)
        {
            if (context.ValidationPlugin == null)
            {
                throw new InvalidOperationException("No validation plugin configured");
            }
            var client = context.Scope.Resolve<AcmeClient>();
            try
            {
                if (context.Valid)
                {
                    _log.Information("[{identifier}] Cached authorization result: {Status}", context.Label, context.Authorization.Status);
                    if (!runLevel.HasFlag(RunLevel.Test) && !runLevel.HasFlag(RunLevel.NoCache))
                    {
                        return;
                    }
                    _log.Information("[{identifier}] Handling challenge anyway because --test and/or --nocache is active", context.Label);
                }

                _log.Information("[{identifier}] Authorizing...", context.Label);
                _log.Verbose("[{identifier}] Initial authorization status: {status}", context.Label, context.Authorization.Status);
                _log.Verbose("[{identifier}] Challenge types available: {challenges}", context.Label, context.Authorization.Challenges?.Select(x => x.Type ?? "[Unknown]"));
                var challenge = context.Authorization.Challenges?.FirstOrDefault(c => string.Equals(c.Type, context.ChallengeType, StringComparison.InvariantCultureIgnoreCase));
                if (challenge == default)
                {
                    if (context.OrderResult.Success == true)
                    {
                        var usedType = context.Authorization.Challenges?.
                            Where(x => x.Status == AcmeClient.ChallengeValid).
                            FirstOrDefault();
                        _log.Warning("[{identifier}] Expected challenge type {type} not available, already validated using {valided}.",
                            context.Label,
                            context.ChallengeType,
                            usedType?.Type ?? "[unknown]");
                        return;
                    }
                    else
                    {
                        _log.Error("[{identifier}] Expected challenge type {type} not available.",
                            context.Label,
                            context.ChallengeType);
                        context.OrderResult.AddErrorMessage("Expected challenge type not available", !context.Valid);
                        return;
                    }
                }
                else
                {
                    _log.Verbose("[{identifier}] Initial challenge status: {status}", context.Label, challenge.Status);
                    if (challenge.Status == AcmeClient.ChallengeValid)
                    {
                        // We actually should not get here because if one of the
                        // challenges is valid, the authorization itself should also 
                        // be valid.
                        if (!runLevel.HasFlag(RunLevel.Test) && !runLevel.HasFlag(RunLevel.NoCache))
                        {
                            _log.Information("[{identifier}] Cached challenge result: {Status}", !context.Valid);
                            return;
                        }
                    }
                }
                _log.Information("[{identifier}] Authorizing using {challengeType} validation ({name})",
                    context.Label,
                    context.ChallengeType,
                    context.PluginName);
                try
                {
                    // Now that we're going to call into PrepareChallenge, we will assume 
                    // responsibility to also call CleanUp later, which is signalled by
                    // the AcmeChallenge propery being not null
                    context.ChallengeDetails = client.DecodeChallengeValidation(context.Authorization, challenge);
                    context.Challenge = challenge;
                    await context.ValidationPlugin.PrepareChallenge(context);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "[{identifier}] Error preparing for challenge answer", context.Label);
                    context.OrderResult.AddErrorMessage("Error preparing for challenge answer", !context.Valid);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error("[{identifier}] Error preparing challenge answer", context.Label);
                var message = _exceptionHandler.HandleException(ex);
                context.OrderResult.AddErrorMessage(message, !context.Valid);
            }
        }

        /// <summary>
        /// After the preparation state, commit the necessary changes.
        /// E.g. for DNS validation, we might have gathered multiple
        /// records that need to be created during the Prepare stage. 
        /// At the Commit stage, the plugin can create them all at the 
        /// same time so that we only have to wait for change propagation
        /// once.
        /// </summary>
        /// <param name="validationPlugin"></param>
        /// <returns></returns>
        private async Task<bool> Commit(IValidationPlugin validationPlugin)
        {
            try
            {
                _log.Verbose("Starting commit stage");
                await validationPlugin.Commit();
                _log.Verbose("Commit was succesful");
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "An error occured while commiting validation configuration: {ex}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Tell the server that we are ready to answer the challenge,
        /// and wait for it to respond with a valid/invalid status.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        private async Task Submit(ValidationContext validationContext)
        {
            if (validationContext.Challenge == null)
            {
                throw new InvalidOperationException("No challenge found");
            }
            try
            {
                _log.Debug("[{identifier}] Submitting challenge answer", validationContext.Label);
                var client = validationContext.Scope.Resolve<AcmeClient>();
                var updatedChallenge = await client.AnswerChallenge(validationContext.Challenge);
                validationContext.Challenge = updatedChallenge;
                if (updatedChallenge.Status != AcmeClient.ChallengeValid)
                {
                    _log.Error("[{identifier}] Authorization result: {Status}", validationContext.Label, updatedChallenge.Status);
                    if (updatedChallenge.Error != null)
                    {
                        _log.Error("[{identifier}] {Error}", validationContext.Label, JsonSerializer.Serialize(updatedChallenge.Error, AcmeClientJson.Default.Problem));
                    }
                    validationContext.OrderResult.AddErrorMessage("Validation failed", !validationContext.Valid);
                    return;
                }
                else
                {
                    // Propagate valid state up to the AcmeAuthorization
                    // This assumption might prove wrong if future 
                    // server implementations require multiple challenges
                    validationContext.Authorization.Status = AcmeClient.AuthorizationValid;
                    _log.Information("[{identifier}] Authorization result: {Status}", validationContext.Label, updatedChallenge.Status);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error("[{identifier}] Error submitting challenge answer", validationContext.Label);
                var message = _exceptionHandler.HandleException(ex);
                validationContext.OrderResult.AddErrorMessage(message, !validationContext.Valid);
            }
        }

        /// <summary>
        /// Clean up after (succesful or unsuccesful) validation attempt
        /// e.g. delete temporary files and DNS records, close any 
        /// listeners.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        private async Task Cleanup(IValidationPlugin validationPlugin)
        {
            try
            {
                _log.Verbose("Starting post-validation cleanup");
                await validationPlugin.CleanUp();
                _log.Verbose("Post-validation cleanup was succesful");
            }
            catch (Exception ex)
            {
                _log.Warning("An error occured during post-validation cleanup: {ex}", ex.Message);
            }
        }
    }
}
