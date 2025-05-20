namespace Idempwanna.Core.Attributes;

/// <summary>
/// Marks a parameter as the source for the idempotency key.
/// When used, the value of this parameter will be used as the idempotency key for the request.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class IdempotentKeyAttribute : Attribute
{
}