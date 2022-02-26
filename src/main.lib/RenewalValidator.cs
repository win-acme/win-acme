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
        /// Answer all the challenges in the order
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="order"></param>
        /// <param name="result"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public async Task AuthorizeOrder(ExecutionContext execution)
        {
            if (execution.Order.Details == null)
            {
                throw new InvalidOperationException();
            }

            // Get authorization details
            var authorizationUris = execution.Order.Details.Payload.Authorizations.ToList();
            var authorizationTasks = authorizationUris.Select(authorizationUri => GetAuthorization(execution, authorizationUri, orderValid));
            var authorizations = (await Task.WhenAll(authorizationTasks)).OfType<acme.Authorization>().ToList();
            if (!execution.Result.Success)
            {
                return;
            }
            
            // Map authorisations to plugins that are going to execute them
            var mapping = new Dictionary<ValidationPluginOptions, List<acme.Authorization>>();
            var add = (ValidationPluginOptions o, acme.Authorization a) => {
                if (mapping.ContainsKey(o))
                {
                    mapping[o].Add(a);
                }
                else
                {
                    mapping.Add(o, new List<acme.Authorization>() { a });
                }
            };
            foreach (var authorization in authorizations)
            {
                var globalOptions = _validationOptions.GetValidationOptions(Identifier.Parse(authorization));
                if (globalOptions != null && IsValid(execution.Scope, authorization, globalOptions))
                {
                    add(globalOptions, authorization);
                } 
                else if (IsValid(execution.Scope, authorization, execution.Renewal.ValidationPluginOptions))
                {
                    add(execution.Renewal.ValidationPluginOptions, authorization);
                }
                else
                {
                    _log.Error("No plugin found that can challenge for {authorisation}", authorization.Identifier.Value);
                    execution.Result.AddErrorMessage($"No plugin found that can challenge for {authorization.Identifier.Value}", !execution.Order.Valid);
                    return;
                }
            }

            // Execute them
            foreach (var group in mapping)
            {
                var scope = _scopeBuilder.Validation(execution.Scope, group.Key);
                var plugin = scope.Resolve<IValidationPlugin>();
                var contexts = group.Value.Select(a =>
                {
                    var targetPart = execution.Target.Parts.FirstOrDefault(p => p.Identifiers.Any(i => i == Identifier.Parse(a)));
                    if (targetPart == null)
                    {
                        throw new InvalidOperationException("Authorisation found that doesn't match target");
                    }
                    return new ValidationContextParameters(a, targetPart, group.Key, execution.Order.Valid);
                }).ToList();
                if (_settings.Validation.DisableMultiThreading != false || plugin.Parallelism == ParallelOperations.None)
                {
                    await SerialValidation(execution, contexts);
                }
                else
                {
                    await ParallelValidation(plugin.Parallelism, scope, execution, contexts);
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
        private bool IsValid(ILifetimeScope scope, acme.Authorization authorization, ValidationPluginOptions options)
        {
            var (plugin, match) = GetInstances(scope, options);
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

            var identifier = Identifier.Parse(authorization);
            var dummyTarget = new Target(identifier);
            if (!match.CanValidate(dummyTarget))
            {
                _log.Warning("Validation plugin {name} cannot validate identifier {identifier}", options.Name, identifier.Value);
                return false;
            }
            if (!authorization.Challenges.Any(x => x.Type == options.ChallengeType))
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
        private async Task ParallelValidation(ParallelOperations level, ILifetimeScope scope, ExecutionContext context, List<ValidationContextParameters> parameters)
        {
            var contexts = parameters.Select(parameter => new ValidationContext(scope, parameter)).ToList();
            var plugin = contexts.First().ValidationPlugin;
            try
            {
                // Prepare for challenge answer
                if (level.HasFlag(ParallelOperations.Prepare))
                {
                    // Parallel
                    _log.Verbose("Handle {n} preparation(s)", contexts.Count);
                    var prepareTasks = contexts.Select(vc => PrepareChallengeAnswer(vc, context.RunLevel));
                    await Task.WhenAll(prepareTasks);
                    foreach (var ctx in contexts)
                    {
                        TransferErrors(ctx, context.Result);
                    }
                    if (!context.Result.Success)
                    {
                        return;
                    }
                }
                else
                {
                    // Serial
                    foreach (var ctx in contexts)
                    {
                        await PrepareChallengeAnswer(ctx, context.RunLevel);
                        TransferErrors(ctx, context.Result);
                        if (!context.Result.Success)
                        {
                            return;
                        }
                    }
                }

                // Commit
                var commited = await CommitValidation(plugin);
                if (!commited)
                {
                    context.Result.AddErrorMessage("Commit failed");
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
                        foreach (var ctx in contextsWithChallenges)
                        {
                            TransferErrors(ctx, context.Result);
                        }
                        if (!context.Result.Success)
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
                            TransferErrors(ctx, context.Result);
                            if (!context.Result.Success)
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
        private async Task SerialValidation(ExecutionContext context, IList<ValidationContextParameters> parameters)
        {
            foreach (var parameter in parameters)
            {
                _log.Verbose("Handle authorization {n}/{m}",
                    parameters.IndexOf(parameter) + 1,
                    parameters.Count);
                using var identifierScope = _scopeBuilder.Validation(context.Scope, parameter.Options);
                await ParallelValidation(ParallelOperations.None, identifierScope, context, new List<ValidationContextParameters> { parameter });
                if (!context.Result.Success)
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
        private async Task<acme.Authorization?> GetAuthorization(ExecutionContext context, string authorizationUri, bool orderValid)
        {
            // Get authorization challenge details from server
            var client = context.Scope.Resolve<AcmeClient>();
            acme.Authorization? authorization;
            try
            {
                authorization = await client.GetAuthorizationDetails(authorizationUri);
            }
            catch
            {
                context.Result.AddErrorMessage($"Unable to get authorization details from {authorizationUri}", !orderValid);
                return null;
            }
            return authorization;
        }

        /// <summary>
        /// Move errors from a validation context up to the renewal result
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="prefix"></param>
        private void TransferErrors(ValidationContext from, RenewResult to)
        {
            from.ErrorMessages.ForEach(e => to.AddErrorMessage($"[{from.Label}] {e}", from.Success != true));
            from.ErrorMessages.Clear();
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
                if (context.Authorization.Status == AcmeClient.AuthorizationValid)
                {
                    context.Success = true;
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
                    if (context.Success == true)
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
                        context.AddErrorMessage("Expected challenge type not available", context.Success == false);
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
                            _log.Information("[{identifier}] Cached challenge result: {Status}", context.Label, context.Authorization.Status);
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
                    context.AddErrorMessage("Error preparing for challenge answer", context.Success == false);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error("[{identifier}] Error preparing challenge answer", context.Label);
                var message = _exceptionHandler.HandleException(ex);
                context.AddErrorMessage(message, context.Success == false);
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
                    validationContext.AddErrorMessage("Validation failed", validationContext.Success != true);
                    return;
                }
                else
                {
                    validationContext.Success = true;
                    _log.Information("[{identifier}] Authorization result: {Status}", validationContext.Label, updatedChallenge.Status);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error("[{identifier}] Error submitting challenge answer", validationContext.Label);
                var message = _exceptionHandler.HandleException(ex);
                validationContext.AddErrorMessage(message, validationContext.Success != true);
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
