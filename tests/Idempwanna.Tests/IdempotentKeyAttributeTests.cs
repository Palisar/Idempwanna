// using Idempwanna.Core.Attributes;
// using Idempwanna.Core.Implementations;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Abstractions;
// using Microsoft.AspNetCore.Mvc.Controllers;
// using Microsoft.AspNetCore.Mvc.Filters;
// using Microsoft.AspNetCore.Routing;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using NSubstitute;
// using Idempwanna.Core.Interfaces;

// namespace Idempwanna.Tests;

// public class IdempotentKeyAttributeTests
// {    private readonly DefaultIdempotencyKeyGenerator _keyGenerator;
//     private readonly IIdempotencyService _mockIdempotencyService;
//     private readonly ILogger<IdempotentAttribute> _mockLogger;
    
//     public IdempotentKeyAttributeTests()
//     {
//         _keyGenerator = new DefaultIdempotencyKeyGenerator();
//         _mockIdempotencyService = Substitute.For<IIdempotencyService>();
//         _mockLogger = Substitute.For<ILogger<IdempotentAttribute>>();
//     }
    
//     [Fact]
//     public async Task OnActionExecutionAsync_ShouldUseParameterValue_WhenMarkedWithIdempotentKeyAttribute()
//     {
//         // Arrange
//         var requestId = Guid.NewGuid();
//         var expectedKey = requestId.ToString();
        
//         var actionArguments = new Dictionary<string, object>
//         {
//             { "requestId", requestId }
//         };
        
//         var parameterInfo = typeof(TestController)
//             .GetMethod(nameof(TestController.TestAction))!
//             .GetParameters()[0];
        
//         var controllerActionDescriptor = new ControllerActionDescriptor
//         {
//             MethodInfo = typeof(TestController).GetMethod(nameof(TestController.TestAction))!,
//             Parameters = new List<ParameterDescriptor>
//             {
//                 new ParameterDescriptor
//                 {
//                     Name = "requestId"
//                 }
//             }
//         };
        
//         var httpContext = new DefaultHttpContext
//         {
//             RequestServices = CreateServiceProvider()
//         };
        
//         var actionContext = new ActionContext(
//             httpContext,
//             new RouteData(),
//             controllerActionDescriptor);
        
//         var filters = new List<IFilterMetadata>();
//         var executedContext = new ActionExecutedContext(actionContext, filters, new TestController());
        
//         bool nextCalled = false;
//         ActionExecutionDelegate next = () =>
//         {
//             nextCalled = true;
//             return Task.FromResult(executedContext);
//         };
//           _mockIdempotencyService
//             .ProcessAsync(
//                 Arg.Is<string>(key => key == expectedKey),
//                 Arg.Any<Func<CancellationToken, Task<IActionResult>>>())
//             .Returns(Task.FromResult<IActionResult>(new OkResult()));
        
//         var context = new ActionExecutingContext(
//             actionContext,
//             filters,
//             actionArguments,
//             new TestController());
        
//         var attribute = new IdempotentAttribute();
        
//         // Act
//         await attribute.OnActionExecutionAsync(context, next);
//           // Assert
//         _mockIdempotencyService
//             .Received(1)
//             .ProcessAsync(
//                 Arg.Is<string>(key => key == expectedKey),
//                 Arg.Any<Func<CancellationToken, Task<IActionResult>>>());
//     }
//       private IServiceProvider CreateServiceProvider()
//     {
//         var services = new ServiceCollection();
        
//         services.AddSingleton<IIdempotencyKeyGenerator>(_keyGenerator);
//         services.AddSingleton(_mockIdempotencyService);
//         services.AddSingleton(_mockLogger);
        
//         return services.BuildServiceProvider();
//     }
//       private class TestController : Controller
//     {
//         public IActionResult TestAction([IdempotentKey] Guid requestId)
//         {
//             return new OkResult();
//         }
//     }
// }
