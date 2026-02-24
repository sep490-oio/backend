namespace OIO.Application.Abstractions.Commons;

public class PagedResult<T> 
{
    public IReadOnlyCollection<T> Items { get; set; }
    public MetaData MetaData { get; set; }

    public PagedResult(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        MetaData = new MetaData
        {
            TotalCount = count,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize)
        };

        Items =  items;
    }

    public static PagedResult<T> ToPagedList(IReadOnlyCollection<T> source, int count, IPagedParameter pagedParameter)
    {

        return new PagedResult<T>(source, count, pagedParameter.PageNumber, pagedParameter.PageSize);
    }
}