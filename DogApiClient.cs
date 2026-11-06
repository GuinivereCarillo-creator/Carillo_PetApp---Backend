namespace Carillo.PetApp.Api;

public class DogApiClient
{
    private readonly HttpClient _http;

    public DogApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<string> GetBreeds(CancellationToken token)
    {
        return GetJson("breeds/list/all", token);
    }

    public Task<string> GetPhoto(string breed, CancellationToken token)
    {
        return GetJson($"breed/{breed}/images/random", token);
    }

    private async Task<string> GetJson(string path, CancellationToken token)
    {
        using var response = await _http.GetAsync(path, token);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(token);
    }
}
