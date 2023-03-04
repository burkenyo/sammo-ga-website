// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    public static RouteHandlerBuilder Produces<TResponse>(this RouteHandlerBuilder builder, int statusCode, string description, IEnumerable<string> contentTypes) =>
        builder.Produces(statusCode, description, typeof(TResponse), null, contentTypes as string[] ?? contentTypes.ToArray());

    public static RouteHandlerBuilder Produces<TResponse>(this RouteHandlerBuilder builder, int statusCode, string description, string? contentType = null, params string[] additionalContentTypes) =>
        builder.Produces(statusCode, description, typeof(TResponse), contentType, additionalContentTypes);


    public static RouteHandlerBuilder Produces(this RouteHandlerBuilder builder, int statusCode, string description, Type? responseType = null, string? contentType = null, params string[] additionalContentTypes) =>
        builder.Produces(statusCode, responseType, contentType, additionalContentTypes)
            .WithOpenApi(operation =>
            {
                operation.Responses[statusCode.ToString()].Description = description;

                return operation;
            });

    public static TBuilder WithParameterDescription<TBuilder>(this TBuilder builder, int id, string description) where TBuilder : IEndpointConventionBuilder =>
        builder.WithOpenApi(operation =>
        {
            operation.Parameters[id].Description = description;

            return operation;
        });

    public static TBuilder WithParameterDescription<TBuilder>(this TBuilder builder, string name, string description) where TBuilder : IEndpointConventionBuilder =>
        builder.WithOpenApi(operation =>
        {
            operation.Parameters.Single(p => p.Name == name).Description = description;

            return operation;
        });

    public static TBuilder WithParameterDescriptions<TBuilder>(this TBuilder builder, params string[] descriptions) where TBuilder : IEndpointConventionBuilder =>
        builder.WithParameterDescriptions((IEnumerable<string>) descriptions);

    public static TBuilder WithParameterDescriptions<TBuilder>(this TBuilder builder, IEnumerable<string> descriptions) where TBuilder : IEndpointConventionBuilder =>
        builder.WithOpenApi(operation =>
        {
            foreach (var (@param, description) in operation.Parameters.Zip(descriptions))
            {
                @param.Description = description;
            }

            return operation;
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

