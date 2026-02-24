namespace OIO.Application.Abstractions.Commons;

public interface IPagedParameter
{
    int PageNumber { get; } 
    int PageSize { get; }
}

public interface IOrderByParameter
{
    string? OrderBy { get; }
}

public interface IDataShapingParameter
{
    string? Fields { get; }
}

[Serializable]
public record PagedParameters : IPagedParameter
{
    const int MaxPageSize = 50;
    private int _pageSize = 10;
    private int _pageNumber = 1;

    public PagedParameters(
        int? pageNumber,
        int? pageSize)
    {
        _pageNumber = pageNumber ?? _pageNumber;
        _pageSize = pageSize ?? _pageSize;
    }

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value;
    }


    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}

public record OrderByParameters
    : PagedParameters, IOrderByParameter
{
    public OrderByParameters(
        string? orderBy,
        int? pageNumber,
        int? pageSize) : base(pageNumber, pageSize)
    {
        OrderBy = orderBy;
    }
    
    public string? OrderBy { get; init; }
}

public record DataShapingParameters
    : OrderByParameters, IDataShapingParameter
{
    public DataShapingParameters(
        string? fields,
        string? orderBy,
        int? pageNumber,
        int? pageSize) : base(orderBy, pageNumber, pageSize)
    {
        Fields = fields;
    }
    
    public string? Fields { get; init; }
}
