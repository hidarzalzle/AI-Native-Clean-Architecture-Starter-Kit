using Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = validators.Select(v => v.Validate(request)).SelectMany(v => v.Errors).Where(f => f is not null).ToList();
        if (failures.Count != 0) throw new ValidationException(failures);
        return await next();
    }
}

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        return await next();
    }
}

public class IdempotencyBehavior<TRequest, TResponse>(ICacheService cache) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand c) return await next();
        var first = await cache.TrySetIdempotencyAsync($"cmd:{c.IdempotencyKey}", TimeSpan.FromHours(1), cancellationToken);
        if (!first) throw new InvalidOperationException("Duplicate command");
        return await next();
    }
}
