using acme = ACMESharp.Protocol.Resources;
using Autofac;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS
{
    /// <summary>
    /// This part of the code handles the actual creation/renewal of ACME certificates
    /// </summary>
    internal class RenewalValidator
    {
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly IValidationOptionsService _validationOptions;
        private readonly ExceptionHandler _exceptionHandler;

        public RenewalValidator(
            IAutofacBuilder scopeBuilder,
            ISettingsService settings,
            ILogService log,
            IValidationOptionsService validationOptions,
            ExceptionHandler exceptionHandler)
        {
            _scopeBuilder = scopeBuilder;
            _log = log;
            _validationOptions = validationOptions;
            _exceptionHandler = exceptionHandler;
            _settings = settings;
        }

        /// <summary>
        /// Authorize multiple orders at once
        /// </summary>
        /// <param name="orderContexts"></param>
        /// <returns></returns>
        internal async Task AuthorizeOrders(IEnumerable<OrderContext> orderContexts, RunLevel runLevel)
        {
            var contextTasks = new List<Task<AuthorizationContext?>>();
            foreach (var orderContext in orderContexts)
            {
                if (orderContext.Order.Details == null)
                {
                    throw new InvalidOperationException();
                }
                
                // Get authorization details
                var authorizationUris = orderContext.Order.Details.Payload.Authorizations.ToList();
                var authorizationTasks = authorizationUris.Select(async uri =>
                {
                    var auth = await GetAuthorization(orderContext, uri);
                    if (auth != null)
                    {
                        return new AuthorizationContext(orderContext, auth);
                    }
                    return null;
                });
                contextTasks.AddRange(authorizationTasks);
            }
            
            // Run all GetAuthorisations in parallel
            var authorizations = await Task.WhenAll(contextTasks);

            // Stop if any of them has failed
            if (orderContexts.Any(x => x.Result.Success == false))
            {
                return;
            }

            // Actually run them
            await RunAuthorizations(authorizations.OfType<AuthorizationContext>(), runLevel);
        }

        /// <summary>
        /// Answer all the challenges in the order
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="order"></param>
        /// <param name="result"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal async Task RunAuthorizations(IEnumerable<AuthorizationContext> authorisationContexts, RunLevel runLevel)
        {
            // Map authorisations to plugins that are going to execute them
            var mapping = new Dictionary<ValidationPluginOptions, List<AuthorizationContext>>();
            var add = (ValidationPluginOptions o, AuthorizationContext a) => {
                if (mapping.ContainsKey(o))
                {
                    mapping[o].Add(a);
                }
                else
                {
                    mapping.Add(o, new List<AuthorizationContext>() { a });
                }
            };
            foreach (var authorisationContext in authorisationContexts)
            {
                if (authorisationContext.Authorization == null)
                {
                    throw new InvalidOperationException();
                }
                var nativeOptions = authorisationContext.Order.Renewal.ValidationPluginOptions;
                var globalOptions = _validationOptions.GetValidationOptions(Identifier.Parse(authorisationContext.Authorization));
                if (globalOptions != null && 
                    IsValid(authorisationContext, globalOptions))
                {
                    add(globalOptions, authorisationContext);
                }
                else if ((globalOptions == null || nativeOptions.Plugin != globalOptions.Plugin) &&
                    IsValid(authorisationContext, nativeOptions))
                {
                    add(nativeOptions, authorisationContext);
                }
                else
                {
                    _log.Error("No plugin found that can challenge for {authorisation}", authorisationContext.Authorization.Identifier.Value);
                    authorisationContext.Order.Result.AddErrorMessage($"No plugin found that can challenge for {authorisationContext.Authorization.Identifier.Value}", authorisationContext.Order.Order.Valid != true);
                    return;
                }
            }

            // Execute them per group, where one group means one validation plugin
            foreach (var group in mapping)
            {
                var validationScope = _scopeBuilder.Validation(group.Value.First().Order.ExecutionScope, group.Key);
                var plugin = validationScope.Resolve<IValidationPlugin>();
                var contexts = group.Value.Select(context =>
                {
                    var targetPart = context.Order.Target.Parts.FirstOrDefault(p => p.Identifiers.Any(i => i == Identifier.Parse(context.Authorization!.Identifier)));
                    if (targetPart == null)
                    {
                        throw new InvalidOperationException("Authorisation found that doesn't match target");
                    }
                    return new ValidationContextParameters(context, targetPart, group.Key);
                }).ToList();
                if (_settings.Validation.DisableMultiThreading != false || plugin.Parallelism == ParallelOperations.None)
                {
                    await SerialValidation(contexts);
                }
                else
                {
                    await ParallelValidation(plugin.Parallelism, validationScope, contexts, runLevel);
                }
            }
        }

        /// <summary>
        /// Get instances of IValidationPlugin and IValidationPluginOptionsFactory
        /// based on an instance of ValidationPluginOptions.
        /// TODO: more cache here
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private (IValidationPlugin?, IValidationPluginOptionsFactory?) GetInstances(ILifetimeScope scope, ValidationPluginOptions options)
        {
            var validationScope = _scopeBuilder.Validation(scope, options);
            try
            {
                var validationPlugin = validationScope.Resolve<IValidationPlugin>();
                var pluginService = scope.Resolve<IPluginService>();
                var match = pluginService.
                    GetFactories<IValidationPluginOptionsFactory>(scope).
                    FirstOrDefault(vp => vp.OptionsType.PluginId() == options.Plugin);
                return (validationPlugin, match);
            } 
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to resolve plugin {options.Name}");
                return (null, null);
            }
        }

        /// <summary>
        /// Will the selected validation plugin be able to validate the 
        /// authorisation?
        /// </summary>
        /// <param name="authorization"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private bool IsValid(AuthorizationContext context, ValidationPluginOptions options)
        {
            var (plugin, match) = GetInstances(context.Order.ExecutionScope, options);
            if (plugin == null || match == null)
            {
                _log.Warning("Validation plugin {name} not found or not created", options.Name);
                return false;
            }
            var (disabled, disabledReason) = plugin.Disabled;
            if (disabled)
            {
                _log.Warning("Validation plugin {name} is not available. {disabledReason}", options.Name, disabledReason);
                return false;
            }

            var identifier = Identifier.Parse(context.Authorization.Identifier);
            var dummyTarget = new Target(identifier);
            if (!match.CanValidate(dummyTarget))
            {
                _log.Warning("Validation plugin {name} cannot validate identifier {identifier}", options.Name, identifier.Value);
                return false;
            }
            if (!context.Authorization.Challenges.Any(x => x.Type == options.ChallengeType))
            {
                _log.Warning("No challenge of type {options.ChallengeType} available", options.Name, identifier.Value);
                return false;
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
                    var prepareTasks = contexts.Select(vc => PrepareChallengeAnswer(vc, runLevel));
                    await Task.WhenAll(prepareTasks);
                    if (contexts.Any(x => x.Result.Success == false))
                    {
                        return;
                    }
                }
                else
                {
                    // Serial
                    foreach (var ctx in contexts)
                    {
                        await PrepareChallengeAnswer(ctx, runLevel);
                        if (ctx.Result.Success == false)
                        {
                            return;
                        }
                    }
                }

                // Commit
                var commited = await CommitValidation(plugin);
                if (!commited)
                {
                    foreach (var ctx in contexts)
                    {
                        ctx.Result.AddErrorMessage("Commit failed");
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
                        var answerTasks = contextsWithChallenges.Select(vc => AnswerChallenge(vc));
                        await Task.WhenAll(answerTasks);
                        if (contextsWithChallenges.Any(x => x.Result.Success == false))
                        {
                            return;
                        }
                    }
                    else
                    {
                        // Serial
                        foreach (var ctx in contextsWithChallenges)
                        {
                            await AnswerChallenge(ctx);
                            if (ctx.Result.Success == false)
                            {
                                return;
                            }
                        }
                    }
                }
            }
            finally
            {
                // Cleanup
                await CleanValidation(plugin);
            }
        }


        /// <summary>
        /// Handle validation in serial order
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task SerialValidation(IList<ValidationContextParameters> parameters)
        {
            foreach (var parameter in parameters)
            {
                _log.Verbose("Handle authorization {n}/{m}",
                    parameters.IndexOf(parameter) + 1,
                    parameters.Count);
                using var validationScope = _scopeBuilder.Validation(parameter.OrderContext.ExecutionScope, parameter.Options);
                await ParallelValidation(ParallelOperations.None, validationScope, new List<ValidationContextParameters> { parameter }, parameter.OrderContext.RunLevel);
                if (parameter.OrderContext.Result.Success == false)
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
        private static async Task<acme.Authorization?> GetAuthorization(OrderContext context, string authorizationUri)
        {
            // Get authorization challenge details from server
            var client = context.ExecutionScope.Resolve<AcmeClient>();
            acme.Authorization? authorization;
            try
            {
                authorization = await client.GetAuthorizationDetails(authorizationUri);
            }
            catch
            {
                context.Result.AddErrorMessage($"Unable to get authorization details from {authorizationUri}", context.Order.Valid != true);
                return null;
            }
            return authorization;
        }

        /// <summary>
        /// Make sure we have authorization for every host in target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private async Task PrepareChallengeAnswer(ValidationContext context, RunLevel runLevel)
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
                    if (!runLevel.HasFlag(RunLevel.Test) && !runLevel.HasFlag(RunLevel.IgnoreCache))
                    {
                        return;
                    }
                    _log.Information("[{identifier}] Handling challenge anyway because --test and/or --force is active", context.Label);
                }

                _log.Information("[{identifier}] Authorizing...", context.Label);
                _log.Verbose("[{identifier}] Initial authorization status: {status}", context.Label, context.Authorization.Status);
                _log.Verbose("[{identifier}] Challenge types available: {challenges}", context.Label, context.Authorization.Challenges.Select(x => x.Type ?? "[Unknown]"));
                var challenge = context.Authorization.Challenges.FirstOrDefault(c => string.Equals(c.Type, context.ChallengeType, StringComparison.InvariantCultureIgnoreCase));
                if (challenge == null)
                {
                    if (context.Result.Success == true)
                    {
                        var usedType = context.Authorization.Challenges.
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
                        context.Result.AddErrorMessage("Expected challenge type not available", !context.Valid);
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
                        if (!runLevel.HasFlag(RunLevel.Test) && !runLevel.HasFlag(RunLevel.IgnoreCache))
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
                    // the Challenge propery being not null
                    context.ChallengeDetails = await client.DecodeChallengeValidation(context.Authorization, challenge);
                    context.Challenge = challenge;
                    await context.ValidationPlugin.PrepareChallenge(context);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "[{identifier}] Error preparing for challenge answer", context.Label);
                    context.Result.AddErrorMessage("Error preparing for challenge answer", !context.Valid);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error("[{identifier}] Error preparing challenge answer", context.Label);
                var message = _exceptionHandler.HandleException(ex);
                context.Result.AddErrorMessage(message, !context.Valid);
            }
        }

        /// <summary>
        /// Make sure we have authorization for every host in target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private async Task AnswerChallenge(ValidationContext validationContext)
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
                        _log.Error("[{identifier}] {Error}", validationContext.Label, updatedChallenge.Error.ToString());

                    }
                    validationContext.Result.AddErrorMessage("Validation failed", !validationContext.Valid);
                    return;
                }
                else
                {
                    _log.Information("[{identifier}] Authorization result: {Status}", validationContext.Label, updatedChallenge.Status);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error("[{identifier}] Error submitting challenge answer", validationContext.Label);
                var message = _exceptionHandler.HandleException(ex);
                validationContext.Result.AddErrorMessage(message, !validationContext.Valid);
            }
        }

        /// <summary>
        /// Clean up after (succesful or unsuccesful) validation attempt
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        private async Task<bool> CommitValidation(IValidationPlugin validationPlugin)
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
        /// Clean up after (succesful or unsuccesful) validation attempt
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        private async Task CleanValidation(IValidationPlugin validationPlugin)
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
