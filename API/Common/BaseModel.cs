using System.Text.Json.Serialization;

namespace API.Common;

/// <summary>
/// Base class for all persistent entities. Provides:
///   - GUID primary key
///   - createdAt / updatedAt timestamps (UTC)
///   - Metadata (JSONB column) for flexible per-record key/value storage
///
/// The Metadata field is useful for storing extension data without a schema change
/// (e.g. UI preferences, integration IDs, feature flags, third-party references).
/// </summary>
public abstract class BaseModel
{
    /// <summary>Unique identifier for the entity.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Timestamp (UTC) the entity was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp (UTC) the entity was last updated.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Arbitrary JSONB column. Use for per-record extension data:
    /// metadata = { "source": "web", "referrer": "newsletter" }.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>Helper for setting a single metadata key without mutating the dictionary reference.</summary>
    public void SetMeta(string key, object? value)
    {
        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Helper for reading a single metadata key.</summary>
    public T? GetMeta<T>(string key) => Metadata.TryGetValue(key, out var v) && v is not null
        ? System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(v))
        : default;
}
