using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Archu.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request execution time for performance monitoring.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

#pragma warning disable CA2016 // Forward cancellation token - RequestHandlerDelegate doesn't accept CancellationToken by design
        var response = await next().ConfigureAwait(false);
#pragma warning restore CA2016

        timer.Stop();

        var elapsedMilliseconds = timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500) // Log if request takes more than 500ms
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogWarning(
                "Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds)",
                requestName,
                elapsedMilliseconds);
        }

        return response;
    }
}
