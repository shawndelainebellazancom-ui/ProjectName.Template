using System.Net.Http.Json;
using ProjectName.Shared.Models;

namespace ProjectName.OrchestrationApi.Clients;

/// <summary>
/// Client for communicating with the Maker microservice.
/// </summary>
/// <param name="client">The injected HttpClient.</param>
public class MakerClient(HttpClient client)
{
    /// <summary>
    /// Sends a plan to the Maker to produce an artifact.
    /// </summary>
    public async Task<Artifact?> ExecuteAsync(Plan plan)
    {
        var response = await client.PostAsJsonAsync("/make", plan);
        return await response.Content.ReadFromJsonAsync<Artifact>();
    }
}

/// <summary>
/// Client for communicating with the Checker microservice.
/// </summary>
/// <param name="client">The injected HttpClient.</param>
public class CheckerClient(HttpClient client)
{
    /// <summary>
    /// Sends an artifact to the Checker for validation.
    /// </summary>
    public async Task<Validation?> ValidateAsync(Artifact artifact)
    {
        var response = await client.PostAsJsonAsync("/check", artifact);
        return await response.Content.ReadFromJsonAsync<Validation>();
    }
}

/// <summary>
/// Client for communicating with the Reflector microservice.
/// </summary>
/// <param name="client">The injected HttpClient.</param>
public class ReflectorClient(HttpClient client)
{
    /// <summary>
    /// Sends validation results to the Reflector for meta-analysis.
    /// </summary>
    public async Task<Reflection?> AnalyzeAsync(Validation validation)
    {
        var response = await client.PostAsJsonAsync("/reflect", validation);
        return await response.Content.ReadFromJsonAsync<Reflection>();
    }
}