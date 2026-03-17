namespace OIO.Application.Abstractions.Commons;
public class PagedList<T>
{
    public IReadOnlyCollection<T> Items { get; set; }
    public Metadata Metadata { get; set; }

    public PagedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        Metadata = new Metadata
        {
            TotalCount = count,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize)
        };

        Items = items;
    }
    public PagedList(IReadOnlyCollection<T> items, Metadata metadata)
    {
        Metadata = metadata;
        Items = items;
    }

    public static PagedList<T> ToPagedList(IReadOnlyCollection<T> source, int count, IPagedParameter pagedParameter)
    {

        return new PagedList<T>(source, count, pagedParameter.PageNumber, pagedParameter.PageSize);
    }
    
}

public static class PagedListExtensions 
{
    extension<T>(IReadOnlyCollection<T> source)
    {
        public PagedList<T> ToPagedList(int count, IPagedParameter pagedParameter)
        {
            return new PagedList<T>(source, count, pagedParameter.PageNumber, pagedParameter.PageSize);
        }
        
        public PagedList<T> ToPagedList(Metadata metadata)
        {
            return new PagedList<T>(source, metadata);
        }
    }
    
    
}