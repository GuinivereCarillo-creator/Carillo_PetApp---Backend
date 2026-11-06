using System.Text.RegularExpressions;
using Carillo.PetApp.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<DogApiClient>(client =>
{
    var baseUrl = builder.Configuration["DogApi:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("DogApi:BaseUrl is missing in appsettings.json.");
    }

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var origins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(child => child.Value ?? string.Empty)
    .Where(value => value.Length > 0)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebApp", policy =>
    {
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("WebApp");

var breedPattern = new Regex("^[a-z]+(/[a-z]+)?$");

app.MapGet("/api/breeds", async (DogApiClient dogs, CancellationToken token) =>
{
    try
    {
        var json = await dogs.GetBreeds(token);
        return Results.Content(json, "application/json");
    }
    catch (HttpRequestException)
    {
        return Results.Json(
            new { message = "The Dog API is not reachable right now." },
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (TaskCanceledException)
    {
        return Results.Json(
            new { message = "The Dog API took too long to answer." },
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
});

app.MapGet("/api/photo", async (string? breed, DogApiClient dogs, CancellationToken token) =>
{
    if (string.IsNullOrWhiteSpace(breed) || !breedPattern.IsMatch(breed))
    {
        return Results.BadRequest(new { message = "That breed is not valid." });
    }

    try
    {
        var json = await dogs.GetPhoto(breed, token);
        return Results.Content(json, "application/json");
    }
    catch (HttpRequestException)
    {
        return Results.Json(
            new { message = "The photo could not be loaded from the Dog API." },
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (TaskCanceledException)
    {
        return Results.Json(
            new { message = "The Dog API took too long to answer." },
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
});

app.Run();
