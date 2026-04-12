namespace OIO.Application.Abstractions.Search;

/// <summary>
/// Shared base search result document used across queries and endpoints.
/// The concrete indexed types in Infrastructure extend this class,
/// but the Application layer only needs this thin surface for queries/DTOs.
/// </summary>
public class BaseSearchDocument
{
    public string Id { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
    
    // Common display fields used by the UI
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? EntityUrl { get; init; }
    
    // Filterable fields shared across entity types
    public string? Status { get; init; }
    public string? CategoryName { get; init; }
    public string? SellerId { get; init; }
    public string? SellerName { get; init; }
    
    // Auto-complete suggestions (list of inputs for Completion Suggester)
    public List<string>? Suggest { get; set; }
}
