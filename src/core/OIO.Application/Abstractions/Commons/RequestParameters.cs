using System.ComponentModel.DataAnnotations;

namespace OIO.Application.Abstractions.Commons;

public interface IPagedParameter
{
    int PageNumber { get; } 
    int PageSize { get; }
}

public interface ISortByParameter
{
    string? SortBy { get; }
}

public interface IDataShapingParameter
{
    string? Fields { get; }
}

[Serializable]
public record PagedParameters : IPagedParameter
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }

    int IPagedParameter.PageNumber => NormalizePageNumber(PageNumber);
    int IPagedParameter.PageSize => NormalizePageSize(PageSize);

    public int EffectivePageNumber => NormalizePageNumber(PageNumber);
    public int EffectivePageSize => NormalizePageSize(PageSize);

    private static int NormalizePageNumber(int? value)
        => value is null or <= 0 ? DefaultPageNumber : value.Value;

    private static int NormalizePageSize(int? value)
        => value is null or <= 0 ? DefaultPageSize : Math.Min(value.Value, MaxPageSize);
}

// public record OrderByParameters
//     : PagedParameters, IOrderByParameter
// {
//     public OrderByParameters(
//         string? orderBy,
//         int? pageNumber,
//         int? pageSize) : base(pageNumber, pageSize)
//     {
//         OrderBy = orderBy;
//     }
//     
//     public string? OrderBy { get; init; }
// }
//
// public record DataShapingParameters
//     : OrderByParameters, IDataShapingParameter
// {
//     public DataShapingParameters(
//         string? fields,
//         string? orderBy,
//         int? pageNumber,
//         int? pageSize) : base(orderBy, pageNumber, pageSize)
//     {
//         Fields = fields;
//     }
//     
//     public string? Fields { get; init; }
// }
