// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

namespace Sammo.Oeis.Api;

class UtilityEndpoints : IWebApi
{
    public static void MapRoutes(IEndpointRouteBuilder builder, Config.CorsConfig corsConfig)
    {
        var group = builder.MapGroup("/")
            .ExcludeFromDescription(); //prevent showing up in swagger

        // redirect requests to the root to the swagger UI
        group.MapGet("/", () => Results.Redirect("/swagger", preserveMethod: true));
        group.MapGet("gitInfo", () => new GitInfoDto());
        // use a “simple request” HTTP method to prevent CORS pre-flight
        group.MapPost("ping", () => Results.NoContent())
            .AddCors(corsConfig);
    }

    // Private constructor: This class is not meant to be instantiated,
    // but since it implements an interface, it cannot be marked static.
    UtilityEndpoints() { }
}
