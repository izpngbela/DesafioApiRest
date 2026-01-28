using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace DesafioApiRest.Api.Configuration;

/// Configuração de políticas de Rate Limiting
public static class RateLimitingConfiguration
{
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Política padrão para a API
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    });
            });

            // Política estrita para login (proteção contra brute force)
            options.AddPolicy("LoginPolicy", context =>
            {
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 3,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    });
            });

            // Política para operações de arquivo
            options.AddPolicy("FilePolicy", context =>
            {
                return RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 20,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        TokensPerPeriod = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Muitas requisições",
                        message = $"Limite de taxa excedido. Tente novamente após {retryAfter.TotalSeconds:F0} segundos.",
                        retryAfter = retryAfter.TotalSeconds
                    }, cancellationToken: cancellationToken);
                }
                else
                {
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Muitas requisições",
                        message = "Limite de taxa excedido. Tente novamente mais tarde."
                    }, cancellationToken: cancellationToken);
                }
            };
        });

        return services;
    }
}
