using System.Text.Json.Serialization;

namespace Azulyoro.Infrastructure.ApiFootball;

/// <summary>Subset of the API-Football /fixtures?id= item used by live sync.</summary>
public class ApiFixtureItem
{
    [JsonPropertyName("fixture")]
    public ApiFixtureCore Fixture { get; set; } = new();

    [JsonPropertyName("goals")]
    public ApiGoals Goals { get; set; } = new();

    [JsonPropertyName("events")]
    public List<ApiFixtureEvent> Events { get; set; } = new();
}

public class ApiFixtureCore
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("status")]
    public ApiFixtureStatus Status { get; set; } = new();
}

public class ApiFixtureStatus
{
    [JsonPropertyName("short")]
    public string? Short { get; set; }

    [JsonPropertyName("elapsed")]
    public int? Elapsed { get; set; }
}

public class ApiGoals
{
    [JsonPropertyName("home")]
    public int? Home { get; set; }

    [JsonPropertyName("away")]
    public int? Away { get; set; }
}

public class ApiFixtureEvent
{
    [JsonPropertyName("time")]
    public ApiEventTime Time { get; set; } = new();

    [JsonPropertyName("team")]
    public ApiEventRef Team { get; set; } = new();

    [JsonPropertyName("player")]
    public ApiEventRef Player { get; set; } = new();

    [JsonPropertyName("assist")]
    public ApiEventRef Assist { get; set; } = new();

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("comments")]
    public string? Comments { get; set; }
}

public class ApiEventTime
{
    [JsonPropertyName("elapsed")]
    public int Elapsed { get; set; }

    [JsonPropertyName("extra")]
    public int? Extra { get; set; }
}

public class ApiEventRef
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
