// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Logging;

namespace Sammo.Oeis.Api;

static class LoggerExtensions
{
    [SuppressMessage("Usage", "CA2254:UseStaticFormatStrings", Justification = "Format string is built dynamically.")]
    public static void Log(
        this ILogger logger, LogLevel logLevel, string message, params (string label, object? value)[] details)
    {
        var detailsToInclude = details.Where(static p => p.value is not null);
        var detailsFormatString = String.Join("; ", detailsToInclude.Select(static d => d.label));
        var detailsValues = detailsToInclude.Select(static d => d.value).ToArray();

        logger.Log(logLevel, message + detailsFormatString, detailsValues);
    }
}

static class EndpointConventionBuilderExtensions
{
     public static RouteHandlerBuilder Produces<TResponse>(this RouteHandlerBuilder builder, int statusCode, string description) =>
         builder.Produces(statusCode, typeof(TResponse))
             .AddOpenApiOperationTransformer((operation, _, _) =>
             {
                 operation.Responses![statusCode.ToString()].Description = description;

                 return Task.CompletedTask;
             });

     public static TBuilder WithParameterDescription<TBuilder>(this TBuilder builder, string name, string description) where TBuilder : IEndpointConventionBuilder =>
         builder.AddOpenApiOperationTransformer((operation, _, _) =>
         {
             operation.Parameters!.Single(p => p.Name == name).Description = description;

             return Task.CompletedTask;
         });
}

interface IWebApi
{
    static abstract void MapRoutes(IEndpointRouteBuilder builder, Config.CorsConfig corsConfig);
}

static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebApi<TApi>(this IServiceCollection services) where TApi : class, IWebApi
    {
        services.TryAddScoped<TApi>();

        return services;
    }

    public static IServiceCollection AddHttpClientWithMinimalLogger<TClient, TImplementation>(this IServiceCollection services)
        where TClient : class where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>()
            .RemoveAllLoggers()
            .AddLogger(provider =>
                new MinimalHttpClientLogger(provider.GetRequiredService<ILogger<TImplementation>>()));

        return services;
    }

    class MinimalHttpClientLogger : IHttpClientLogger
    {
        readonly ILogger _logger;

        internal MinimalHttpClientLogger(ILogger logger)
        {
            _logger = logger;
        }

        public object? LogRequestStart(HttpRequestMessage request) =>
            // client may modify the request URI in the case of redirects, examine the original here
            request.RequestUri;

        public void LogRequestStop(object? requestUri, HttpRequestMessage request, HttpResponseMessage response,
            TimeSpan elapsed)
        {
            var numericStatusCode = (int)response.StatusCode;
            var logLevel = numericStatusCode < 400 ? LogLevel.Information : LogLevel.Warning;

            _logger.Log(logLevel, "{Method} {Uri} - {StatusCode} ({StatusCodeLiteral}) in {Time}ms",
                request.Method, requestUri, numericStatusCode, response.StatusCode, elapsed.TotalMilliseconds);
        }

        public void LogRequestFailed(object? requestUri, HttpRequestMessage request, HttpResponseMessage? response,
            Exception exception, TimeSpan elapsed)
        {
            _logger.LogError(exception, "{Method} {Uri} failed in {Time}ms!",
                request.Method, requestUri, elapsed.TotalMilliseconds);
        }
    }
}

static class EndpointRouteBuilderExtensions
{
    public static void Map<TApi>(this IEndpointRouteBuilder builder, Config.CorsConfig corsConfig)
        where TApi : class, IWebApi
    {
        TApi.MapRoutes(builder, corsConfig);
    }

    /// <summary>
    /// Add a CORS policy to the <see cref="IEndpointConventionBuilder" /> with defaults provided by the
    /// <see cref="Config.CorsConfig" />. The provided <see cref="Action{T}" /> is used to further configure the policy.
    /// </summary>
    public static void AddCors(this IEndpointConventionBuilder builder,
        Config.CorsConfig config, Action<CorsPolicyBuilder>? configurePolicy = null)
    {
        if (config.UseCors)
        {
            builder.RequireCors(policy =>
            {
                if (config.AllowAnyOrigin)
                {
                    policy.AllowAnyOrigin();
                }
                else
                {
                    policy.WithOrigins(config.GetAllowedOrigins());
                }

                if (configurePolicy is not null)
                {
                    configurePolicy(policy);
                }
            });
        }
    }
}

static class WebHostEnvironmentExtensions
{
    public static bool IsRunningInContainer(this IWebHostEnvironment env) =>
        Convert.ToBoolean(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));
}

