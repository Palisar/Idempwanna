using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Idempwanna.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;

namespace Idempwanna.Core.Attributes;

/// <summary>
/// Attribute used to mark controller actions as idempotent.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _headerName;
    private readonly bool _requiresKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotentAttribute"/> class.
    /// </summary>
    /// <param name="headerName">The name of the header to use for idempotency keys.</param>
    /// <param name="requiresKey">Whether an idempotency key is required.</param>
    public IdempotentAttribute(string headerName = "x-idempotency-key", bool requiresKey = true)
    {
        _headerName = headerName;
        _requiresKey = requiresKey;
    }    /// <summary>
    /// Called asynchronously before the action, after model binding is complete.
    /// </summary>
    /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
    /// <param name="next">The <see cref="ActionExecutionDelegate"/>. Invoked to execute the next action filter or the action itself.</param>
    /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var keyGenerator = context.HttpContext.RequestServices.GetRequiredService<IIdempotencyKeyGenerator>();
        var idempotencyService = context.HttpContext.RequestServices.GetRequiredService<IIdempotencyService>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<IdempotentAttribute>>();

        string? idempotencyKey = null;

        // First, check if any parameter is marked with IdempotentKeyAttribute
        if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
        {
            var parameterInfos = actionDescriptor.MethodInfo.GetParameters();
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var parameter = parameterInfos[i];
                if (parameter.GetCustomAttribute<IdempotentKeyAttribute>() != null)
                {
                    if (context.ActionArguments.TryGetValue(parameter.Name!, out var parameterValue) && parameterValue != null)
                    {
                        idempotencyKey = keyGenerator.GetKeyFromParameter(parameterValue);
                        logger.LogInformation("Using parameter '{ParameterName}' with value '{ParameterValue}' as idempotency key", 
                            parameter.Name, parameterValue);
                        break;
                    }
                }
            }
        }

        // If no parameter with IdempotentKeyAttribute found, try to extract from request
        if (idempotencyKey == null)
        {
            try
            {
                idempotencyKey = await keyGenerator.ExtractFromHttpRequestAsync(context.HttpContext.Request, _headerName);
            }
            catch (InvalidOperationException ex)
            {
                if (_requiresKey)
                {
                    logger.LogWarning(ex, "Failed to extract idempotency key from request");
                    context.Result = new BadRequestObjectResult($"Missing idempotency key. Please provide a '{_headerName}' header.");
                    return;
                }

                // If key is not required, proceed with the action
                logger.LogInformation("No idempotency key found, but it's not required. Proceeding with action execution.");
                await next();
                return;
            }
        }

        try
        {
            // Use the idempotency service to process the request
            var result = await idempotencyService.ProcessAsync<IActionResult>(
                idempotencyKey,
                async cancellationToken =>
                {
                    var actionExecutedContext = await next();
                    return actionExecutedContext.Result;
                });

            // Set the result directly
            context.Result = result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing idempotent request with key: {IdempotencyKey}", idempotencyKey);
            throw;
        }
    }
}
