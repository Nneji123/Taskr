using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Common.Swagger;

/// <summary>
/// Adds human-readable descriptions to each Swagger tag so the UI shows
/// a short blurb under the section heading (e.g. "Authentication",
/// "Projects", "Tasks", "Files").
/// </summary>
public sealed class TagDescriptionsDocumentFilter : IDocumentFilter
{
    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.Ordinal)
    {
        ["Auth"] = "Account creation, login, refresh, and password recovery.",
        ["Projects"] = "Project lifecycle: list, create, read, update, and delete.",
        ["Tasks"] = "Task lifecycle scoped to a project: list, create, read, update, delete.",
        ["Files"] = "Multipart file upload and deletion for user-uploaded assets.",
        ["Health"] = "Liveness and dependency health probes.",
        ["API"] = "Root and utility endpoints."
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Swashbuckle populates Tags from controller names. Merge our
        // descriptions in by name (overwriting if present) and keep the
        // order stable.
        var byName = (swaggerDoc.Tags ?? new List<OpenApiTag>())
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        foreach (var (name, description) in Descriptions)
        {
            if (byName.TryGetValue(name, out var existing))
            {
                existing.Description = description;
            }
            else
            {
                byName[name] = new OpenApiTag { Name = name, Description = description };
            }
        }

        swaggerDoc.Tags = byName.Values
            .OrderBy(t => Descriptions.ContainsKey(t.Name) ? Descriptions.Keys.ToList().IndexOf(t.Name) : int.MaxValue)
            .ToList();
    }
}
