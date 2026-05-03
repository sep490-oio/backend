using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.AssistantContext.Services;
using OIO.Application.Context.AssistantContext.Services.Models;

namespace OIO.Infrastructure.Assistant;

internal sealed class KnowledgeBaseRetriever : IAssistantKnowledgeRetriever
{
    private readonly IReadOnlyList<KnowledgeEntry> _entries;
    private readonly ILogger<KnowledgeBaseRetriever> _logger;

    public KnowledgeBaseRetriever(ILogger<KnowledgeBaseRetriever> logger, string? customPath = null)
    {
        _logger = logger;
        _entries = LoadEntries(customPath);
    }

    public IReadOnlyList<KnowledgeEntry> Retrieve(string query, int topN = 3)
    {
        if (string.IsNullOrWhiteSpace(query) || _entries.Count == 0)
            return [];

        var tokens = Tokenize(query);
        if (tokens.Count == 0)
            return [];

        var scored = _entries
            .Select(entry => new
            {
                Entry = entry,
                Score = ScoreEntry(entry, tokens)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .Select(x => x.Entry)
            .ToList();

        return scored;
    }

    private static int ScoreEntry(KnowledgeEntry entry, IReadOnlyCollection<string> tokens)
    {
        var titleNormalized = Normalize(entry.Title);
        var contentNormalized = Normalize(entry.Content + " " + entry.Summary);
        var tagsNormalized = entry.Tags.Select(Normalize).ToList();

        var score = 0;
        foreach (var token in tokens)
        {
            if (tagsNormalized.Any(t => t.Contains(token)))
                score += 3;
            if (titleNormalized.Contains(token))
                score += 2;
            if (contentNormalized.Contains(token))
                score += 1;
        }
        return score;
    }

    private static List<string> Tokenize(string text)
    {
        var normalized = Normalize(text);
        return normalized
            .Split([' ', ',', '.', '?', '!', ';', ':', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2)
            .Distinct()
            .ToList();
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        // Lowercase + strip Vietnamese diacritics for tolerant matching.
        var formD = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString().Replace('đ', 'd').Replace('Đ', 'd');
    }

    private IReadOnlyList<KnowledgeEntry> LoadEntries(string? customPath)
    {
        try
        {
            // Locate the JSON file: prefer customPath if provided, else next to the assembly.
            var path = customPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                path = Path.Combine(assemblyDir, "Assistant", "Knowledge", "assistant-knowledge.json");
                if (!File.Exists(path))
                {
                    // Fall back to project-relative path during dev (content not copied).
                    var sourceCandidate = Path.Combine(
                        AppContext.BaseDirectory,
                        "Assistant",
                        "Knowledge",
                        "assistant-knowledge.json");
                    if (File.Exists(sourceCandidate))
                        path = sourceCandidate;
                }
            }

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                _logger.LogWarning(
                    "Assistant knowledge file not found at {Path}. Knowledge retrieval will return empty.",
                    path);
                return [];
            }

            var json = File.ReadAllText(path);
            var entries = JsonSerializer.Deserialize<List<KnowledgeEntry>>(
                json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
            return entries ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assistant knowledge base");
            return [];
        }
    }
}
