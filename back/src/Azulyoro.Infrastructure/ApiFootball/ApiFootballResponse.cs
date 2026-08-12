using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azulyoro.Infrastructure.ApiFootball;

/// <summary>
/// Generic API-Football envelope. `errors` is polymorphic in their API (an
/// empty array on success, an object on failure), so it is captured as a raw
/// <see cref="JsonElement"/> and inspected via <see cref="HasErrors"/>.
/// </summary>
public class ApiFootballResponse<T>
{
    [JsonPropertyName("results")]
    public int Results { get; set; }

    [JsonPropertyName("paging")]
    public ApiFootballPaging? Paging { get; set; }

    [JsonPropertyName("errors")]
    public JsonElement Errors { get; set; }

    [JsonPropertyName("response")]
    public List<T> Response { get; set; } = new();

    public bool HasErrors =>
        Errors.ValueKind == JsonValueKind.Object && Errors.EnumerateObject().Any();

    public string? ErrorText =>
        HasErrors ? Errors.GetRawText() : null;
}

public class ApiFootballPaging
{
    [JsonPropertyName("current")]
    public int Current { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
