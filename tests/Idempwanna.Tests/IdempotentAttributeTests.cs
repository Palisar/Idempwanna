using FluentAssertions;
using Idempwanna.Core.Attributes;
using Idempwanna.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Idempwanna.Tests;

public class IdempotentAttributeTests
{
    private readonly IIdempotencyService _idempotencyService;
    private readonly IIdempotencyKeyGenerator _keyGenerator;
    private readonly ILogger<IdempotentAttribute> _logger;
    private readonly ActionExecutionDelegate _next;

    public IdempotentAttributeTests()
    {
        _idempotencyService = Substitute.For<IIdempotencyService>();
        _keyGenerator = Substitute.For<IIdempotencyKeyGenerator>();
        _logger = Substitute.For<ILogger<IdempotentAttribute>>();
        _next = Substitute.For<ActionExecutionDelegate>();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldExecuteNext_WhenNoIdempotencyKey()
    {
        // Arrange
        var attribute = new IdempotentAttribute();
        var actionContext = CreateActionContext();
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller: null);

        // Mock key generator to return null/empty
        _keyGenerator.ExtractFromHttpRequestAsync(
                Arg.Any<HttpRequest>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(string.Empty));

        var services = new ServiceCollection();
        services.AddSingleton(_idempotencyService);
        services.AddSingleton(_keyGenerator);
        services.AddSingleton(_logger);
        
        var serviceProvider = services.BuildServiceProvider();
        context.HttpContext.RequestServices = serviceProvider;

        var nextCalled = false;
        _next.Invoke().Returns(Task.FromResult(new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            controller: null)));

        // Act
        await attribute.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            return _next();
        });

        // Assert
        nextCalled.Should().BeTrue();
        await _idempotencyService.DidNotReceive().ProcessAsync<ActionExecutedContext>(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, Task<ActionExecutedContext>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldUseIdempotencyService_WhenKeyPresent()
    {
        // Arrange
        var attribute = new IdempotentAttribute();
        var actionContext = CreateActionContext();
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller: null);

        // Mock key generator to return a key
        const string idempotencyKey = "test-idempotency-key";
        _keyGenerator.ExtractFromHttpRequestAsync(
                Arg.Any<HttpRequest>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(idempotencyKey));

        // Mock result from idempotency service
        var expectedResult = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            controller: null)
        {
            Result = new OkObjectResult("Cached result")
        };

        _idempotencyService.ProcessAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<ActionExecutedContext>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        var services = new ServiceCollection();
        services.AddSingleton(_idempotencyService);
        services.AddSingleton(_keyGenerator);
        services.AddSingleton(_logger);
        
        var serviceProvider = services.BuildServiceProvider();
        context.HttpContext.RequestServices = serviceProvider;

        // Act
        var result = await attribute.OnActionExecutionAsync(context, _next);

        // Assert
        result.Should().BeSameAs(expectedResult);
        await _idempotencyService.Received(1).ProcessAsync<ActionExecutedContext>(
            Arg.Is<string>(key => key == idempotencyKey),
            Arg.Any<Func<CancellationToken, Task<ActionExecutedContext>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldUseCustomHeader_WhenProvided()
    {
        // Arrange
        const string customHeader = "X-Custom-Idempotency";
        var attribute = new IdempotentAttribute(customHeader);
        var actionContext = CreateActionContext();
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller: null);

        // Mock key generator to return a key
        const string idempotencyKey = "test-idempotency-key";
        _keyGenerator.ExtractFromHttpRequestAsync(
                Arg.Any<HttpRequest>(),
                Arg.Is<string>(header => header == customHeader),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(idempotencyKey));

        var services = new ServiceCollection();
        services.AddSingleton(_idempotencyService);
        services.AddSingleton(_keyGenerator);
        services.AddSingleton(_logger);
        
        var serviceProvider = services.BuildServiceProvider();
        context.HttpContext.RequestServices = serviceProvider;

        // Act
        await attribute.OnActionExecutionAsync(context, _next);

        // Assert
        await _keyGenerator.Received(1).ExtractFromHttpRequestAsync(
            Arg.Any<HttpRequest>(),
            Arg.Is<string>(header => header == customHeader),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldThrowException_WhenRequireKeyIsTrue_AndNoKeyProvided()
    {
        // Arrange
        var attribute = new IdempotentAttribute(requiresKey: true);
        var actionContext = CreateActionContext();
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller: null);

        // Mock key generator to return null/empty
        _keyGenerator.ExtractFromHttpRequestAsync(
                Arg.Any<HttpRequest>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(string.Empty));

        var services = new ServiceCollection();
        services.AddSingleton(_idempotencyService);
        services.AddSingleton(_keyGenerator);
        services.AddSingleton(_logger);
        
        var serviceProvider = services.BuildServiceProvider();
        context.HttpContext.RequestServices = serviceProvider;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            attribute.OnActionExecutionAsync(context, _next));
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldExecuteOperation_WhenCacheDoesntHaveResult()
    {
        // Arrange
        var attribute = new IdempotentAttribute();
        var actionContext = CreateActionContext();
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller: null);

        // Mock key generator to return a key
        const string idempotencyKey = "new-operation-key";
        _keyGenerator.ExtractFromHttpRequestAsync(
                Arg.Any<HttpRequest>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(idempotencyKey));

        // Setup the next delegate to return a result
        var originalResult = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            controller: null)
        {
            Result = new OkObjectResult("Original result")
        };

        _next.Invoke().Returns(Task.FromResult(originalResult));

        // Setup idempotency service to execute the operation
        _idempotencyService
            .When(x => x.ProcessAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<ActionExecutedContext>>>(),
                Arg.Any<CancellationToken>()))
            .Do(callInfo => 
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<ActionExecutedContext>>>();
                return operation(CancellationToken.None);
            });

        var services = new ServiceCollection();
        services.AddSingleton(_idempotencyService);
        services.AddSingleton(_keyGenerator);
        services.AddSingleton(_logger);
        
        var serviceProvider = services.BuildServiceProvider();
        context.HttpContext.RequestServices = serviceProvider;

        // Act
        var result = await attribute.OnActionExecutionAsync(context, _next);

        // Assert
        result.Result.Should().BeSameAs(originalResult.Result);
        await _next.Received(1).Invoke();
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };
    }
}
