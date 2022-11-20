using System.Diagnostics.CodeAnalysis;
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
    static abstract void MapRoutes(IEndpointRouteBuilder builder);
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
    public static void Map<TApi>(this IEndpointRouteBuilder builder) where TApi : class, IWebApi =>
        TApi.MapRoutes(builder);
}

static class WebHostEnvironmentExtensions
{
    public static bool IsRunningInContainer(this IWebHostEnvironment env) =>
        Convert.ToBoolean(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));
}