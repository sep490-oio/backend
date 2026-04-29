using System.Diagnostics;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services.AiSuggestion;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.SuggestItemDescription;

internal sealed class SuggestItemDescriptionCommandHandler
    : ICommandHandler<SuggestItemDescriptionCommand, SuggestItemDescriptionResult>
{
    private const string ItemImageContext = "item_image";
    private const string ImageResourceType = "image";

    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IItemDescriptionSuggestionService _suggester;
    private readonly IAiHtmlSanitizer _htmlSanitizer;
    private readonly IOptions<AiOptions> _options;
    private readonly ILogger<SuggestItemDescriptionCommandHandler> _logger;

    public SuggestItemDescriptionCommandHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IItemDescriptionSuggestionService suggester,
        IAiHtmlSanitizer htmlSanitizer,
        IOptions<AiOptions> options,
        ILogger<SuggestItemDescriptionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _suggester = suggester;
        _htmlSanitizer = htmlSanitizer;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<SuggestItemDescriptionResult, Error>> Handle(
        SuggestItemDescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var opts = _options.Value;
        if (!opts.Enabled)
        {
            return AiSuggestionErrors.Disabled;
        }

        // Enforce configured per-request image cap (default 3).
        if (request.ImageMediaUploadIds.Count > opts.MaxImages)
        {
            return Error.Validation(
                "ImageMediaUploadIds",
                "ImageMediaUploadIds.OutOfRange",
                $"Too many images; maximum is {opts.MaxImages}.");
        }

        var sellerId = _currentUser.UserId;

        var ids = request.ImageMediaUploadIds
            .Select(g => MediaUploadId.From(g))
            .ToList();

        var uploads = await _dbContext.Set<MediaUpload>()
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);

        if (uploads.Count != ids.Count)
        {
            var foundIds = uploads.Select(u => u.Id).ToHashSet();
            var missing = ids
                .Where(id => !foundIds.Contains(id))
                .Select(i => i.Value.ToString())
                .ToArray();

            _logger.LogWarning(
                "AI suggest: media uploads not found. SellerId={SellerId} MissingIds={MissingIds}",
                sellerId, string.Join(", ", missing));

            return MediaErrors.NotFounds(string.Join(", ", missing));
        }

        var allowedContexts = new[] { ItemImageContext };
        foreach (var u in uploads)
        {
            if (u.UserId != sellerId)
                return MediaErrors.NotOwnedByUser(u.Id);
            if (!u.IsConfirmed)
                return MediaErrors.NotConfirm;
            if (u.IsLinked)
                return MediaErrors.AlreadyLinked;
            if (!string.Equals(u.Context, ItemImageContext, StringComparison.OrdinalIgnoreCase))
                return MediaErrors.WrongContext(u.Context, allowedContexts);
            if (!IsImageResource(u.ResourceType))
                return MediaErrors.WrongResourceType(u.ResourceType, ImageResourceType);
            if (string.IsNullOrWhiteSpace(u.Info?.SecureUrl))
                return MediaErrors.NotContainUrl;
        }

        var activeCats = await _dbContext.Set<Category>()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new CategoryReference(c.Id.Value, c.Name, c.Path.Value))
            .ToListAsync(cancellationToken);

        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "vi" : request.Locale!.ToLowerInvariant();

        var input = new SuggestItemDescriptionInput(
            request.Title,
            request.Condition,
            uploads
                .Select(u => new ImageRef(u.Id.Value, u.Info!.SecureUrl!, u.Info?.Format))
                .ToList(),
            activeCats,
            locale);

        _logger.LogInformation(
            "AI suggestion start: SellerId={SellerId} MediaCount={MediaCount} Model={Model} Locale={Locale}",
            sellerId, uploads.Count, opts.Model, locale);

        var sw = Stopwatch.StartNew();
        Result<RawSuggestion, Error> raw;
        try
        {
            raw = await _suggester.SuggestAsync(input, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled — propagate as cancellation, not a provider error.
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "AI provider timed out: SellerId={SellerId} Model={Model} DurationMs={DurationMs}",
                sellerId, opts.Model, sw.ElapsedMilliseconds);
            return AiSuggestionErrors.ProviderUnavailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "AI provider failed: SellerId={SellerId} Model={Model} ExceptionType={ExceptionType} DurationMs={DurationMs}",
                sellerId, opts.Model, ex.GetType().Name, sw.ElapsedMilliseconds);
            return AiSuggestionErrors.ProviderUnavailable;
        }

        sw.Stop();

        if (raw.IsFailure)
        {
            return raw.Error;
        }

        var sanitizedResult = Sanitize(raw.Value, activeCats, opts, _htmlSanitizer);
        if (sanitizedResult.IsFailure)
        {
            _logger.LogWarning(
                "AI suggestion rejected after sanitation: SellerId={SellerId} Reason={Reason}",
                sellerId, sanitizedResult.Error.Code);
            return sanitizedResult.Error;
        }

        var sanitized = sanitizedResult.Value;

        _logger.LogInformation(
            "AI suggestion succeeded: SellerId={SellerId} DurationMs={DurationMs} DescChars={DescChars} CategoryFound={CategoryFound} AltCount={AltCount} Warnings={Warnings}",
            sellerId,
            sw.ElapsedMilliseconds,
            sanitized.Description.Length,
            sanitized.SuggestedCategory != null,
            sanitized.Alternatives.Count,
            string.Join(",", sanitized.Warnings));

        return sanitized;
    }

    private static bool IsImageResource(string? resourceType) =>
        !string.IsNullOrWhiteSpace(resourceType)
        && string.Equals(resourceType, ImageResourceType, StringComparison.OrdinalIgnoreCase);

    private static Result<SuggestItemDescriptionResult, Error> Sanitize(
        RawSuggestion raw,
        IReadOnlyList<CategoryReference> activeCategories,
        AiOptions opts,
        IAiHtmlSanitizer htmlSanitizer)
    {
        var warnings = new List<string>(raw.Warnings ?? Array.Empty<string>());
        var lookup = activeCategories.ToDictionary(c => c.Id);

        // Description sanitation: convert raw model output (HTML or plain text) into a
        // safe HTML fragment, budget by plain-text length, reject empty results.
        var sanitization = htmlSanitizer.Sanitize(raw.Description);
        if (string.IsNullOrWhiteSpace(sanitization.Html)
            || string.IsNullOrWhiteSpace(sanitization.PlainText))
        {
            return AiSuggestionErrors.InvalidModelOutput;
        }

        var safeHtml = sanitization.Html;
        if (sanitization.PlainText.Length > opts.MaxDescriptionChars)
        {
            safeHtml = htmlSanitizer.TruncateToBudget(sanitization.Html, opts.MaxDescriptionChars);
            if (!warnings.Contains("description.truncated"))
            {
                warnings.Add("description.truncated");
            }
        }

        // Suggested category sanitation
        CategorySuggestion? suggested = null;
        if (raw.SuggestedCategoryId.HasValue
            && lookup.TryGetValue(raw.SuggestedCategoryId.Value, out var matched))
        {
            if (raw.SuggestedCategoryConfidence < opts.MinSuggestedConfidence)
            {
                if (!warnings.Contains("category.lowConfidence"))
                {
                    warnings.Add("category.lowConfidence");
                }
            }
            else
            {
                suggested = new CategorySuggestion(
                    matched.Id,
                    matched.Name,
                    matched.Path,
                    raw.SuggestedCategoryConfidence);
            }
        }
        else if (raw.SuggestedCategoryId.HasValue)
        {
            if (!warnings.Contains("category.unmatched"))
            {
                warnings.Add("category.unmatched");
            }
        }
        else if (!warnings.Contains("category.unmatched"))
        {
            warnings.Add("category.unmatched");
        }

        // Alternatives sanitation
        var alternatives = new List<CategorySuggestion>();
        if (raw.Alternatives is not null)
        {
            foreach (var alt in raw.Alternatives)
            {
                if (!lookup.TryGetValue(alt.Id, out var altMatched)) continue;
                if (alt.Confidence < opts.MinAlternativeConfidence) continue;
                if (suggested != null && altMatched.Id == suggested.Id) continue;

                alternatives.Add(new CategorySuggestion(
                    altMatched.Id,
                    altMatched.Name,
                    altMatched.Path,
                    alt.Confidence));
            }
        }

        return new SuggestItemDescriptionResult(
            safeHtml,
            "html",
            suggested,
            alternatives,
            warnings);
    }
}
