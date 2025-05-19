# Idempwanna

Idempwanna is an abstract idempotency feature for ASP.NET projects that can be used with different types of caching offerings in the .NET Core framework.

## What is Idempotency?

Idempotency is the property of certain operations whereby they can be applied multiple times without changing the result beyond the initial application. In web APIs, this means that making the same request multiple times has the same effect as making it once.

## Features

- Simple API for handling idempotent operations
- Support for different caching mechanisms:
  - In-memory caching
  - Custom cache implementations
- Attribute-based approach for marking ASP.NET Core endpoints as idempotent
- Configurable idempotency key handling (header, query parameter, or custom)
- Support for both synchronous and asynchronous operations
- Thread-safe implementation

## Installation

```shell
dotnet add package Idempwanna
```

## Basic Usage

### 1. Register Services

Register the idempotency services in your `Program.cs` or `Startup.cs`:

```csharp
// Using in-memory cache (simplest approach)
builder.Services.AddIdempotencyWithInMemoryCache();

// Or with custom options
builder.Services.AddIdempotencyWithInMemoryCache(options =>
{
    options.DefaultCacheExpiration = TimeSpan.FromHours(1);
    options.DefaultHeaderName = "x-Idempotency-Key";
    options.ThrowOnMissingKey = true;
});

// Or with a custom cache implementation
builder.Services.AddIdempotencyWithCustomCache<YourCustomCacheImplementation>();
```

### 2. Mark Endpoints as Idempotent

Use the `[Idempotent]` attribute on your controller actions:

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    [Idempotent] // This makes the endpoint idempotent
    public async Task<ActionResult<OrderResponse>> CreateOrder(OrderRequest request)
    {
        // Your implementation here
        // The idempotency is handled automatically by the attribute
        return Ok(new OrderResponse { /* ... */ });
    }
}
```

### 3. Manual Usage

If you need more control, you can use the `IIdempotencyService` directly:

```csharp
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IIdempotencyService _idempotencyService;
    private readonly IIdempotencyKeyGenerator _keyGenerator;
    
    public PaymentsController(
        IIdempotencyService idempotencyService,
        IIdempotencyKeyGenerator keyGenerator)
    {
        _idempotencyService = idempotencyService;
        _keyGenerator = keyGenerator;
    }
    
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> ProcessPayment(PaymentRequest request)
    {
        // Generate a key from the request
        var idempotencyKey = _keyGenerator.GenerateKey(request);
        
        // Use the idempotency service to process the request
        var response = await _idempotencyService.ProcessAsync<PaymentResponse>(
            idempotencyKey,
            async cancellationToken =>
            {
                // This will only execute if this is not a duplicate request
                PaymentResponse result = await ProcessPaymentLogic(request);
                return result;
            });
            
        return Ok(response);
    }
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
