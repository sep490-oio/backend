using System.Text.Json;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AuctionContext.Services.AiSuggestion;

namespace OIO.Infrastructure.Ai;

internal static class SuggestionResponseParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static (RawSuggestion? Result, string? Error) Parse(string text, AiOptions opts)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, "Empty response.");
        }

        var json = ExtractJsonObject(text);
        if (json is null)
        {
            return (null, "No JSON object found in response.");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Prefer the new descriptionHtml field; fall back to legacy plain `description`
            // so older / off-format model outputs still work. The handler sanitizes both.
            var description = TryGetString(root, "descriptionHtml")
                ?? TryGetString(root, "description")
                ?? string.Empty;

            Guid? suggestedId = null;
            if (root.TryGetProperty("suggestedCategoryId", out var sid)
                && sid.ValueKind != JsonValueKind.Null
                && Guid.TryParse(sid.GetString(), out var parsedId))
            {
                suggestedId = parsedId;
            }

            var suggestedConfidence = TryGetDouble(root, "suggestedCategoryConfidence") ?? 0.0;

            var alternatives = new List<RawAlternative>();
            if (root.TryGetProperty("alternatives", out var alts) && alts.ValueKind == JsonValueKind.Array)
            {
                foreach (var alt in alts.EnumerateArray())
                {
                    if (alt.ValueKind != JsonValueKind.Object) continue;
                    if (!alt.TryGetProperty("id", out var idProp)) continue;
                    if (!Guid.TryParse(idProp.GetString(), out var altId)) continue;

                    var altConfidence = TryGetDouble(alt, "confidence") ?? 0.0;
                    alternatives.Add(new RawAlternative(altId, altConfidence));
                }
            }

            var warnings = new List<string>();
            if (root.TryGetProperty("warnings", out var warns) && warns.ValueKind == JsonValueKind.Array)
            {
                foreach (var w in warns.EnumerateArray())
                {
                    if (w.ValueKind == JsonValueKind.String)
                    {
                        var s = w.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            warnings.Add(s!);
                        }
                    }
                }
            }

            var raw = new RawSuggestion(
                description,
                suggestedId,
                suggestedConfidence,
                alternatives,
                warnings);

            return (raw, null);
        }
        catch (JsonException)
        {
            return (null, "Malformed JSON in response.");
        }
    }

    private static string? TryGetString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
    }

    private static double? TryGetDouble(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var d)) return d;
        if (prop.ValueKind == JsonValueKind.String
            && double.TryParse(prop.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ds))
        {
            return ds;
        }
        return null;
    }

    /// <summary>
    /// Extract the first JSON object substring from the text. Some providers
    /// wrap JSON in markdown fences despite instructions; this is defensive.
    /// </summary>
    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0) return null;
        var depth = 0;
        for (var i = start; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '{': depth++; break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(start, i - start + 1);
                    }
                    break;
            }
        }
        return null;
    }
}
