using System.Linq.Dynamic.Core;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Sorting;

namespace OIO.Application.Extensions;

public static class QueryableExtensions
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> Page(IPagedParameter pagedParameter)
        {
            return query
                .Skip((pagedParameter.PageNumber - 1) * pagedParameter.PageSize)
                .Take(pagedParameter.PageSize);
        }

        public IQueryable<T> ApplySort(
            ISortByParameter orderByParameter,
            SortMapping[] mappings,
            string defaultOrderBy = "Id")
        {
            if (string.IsNullOrWhiteSpace(orderByParameter.SortBy))
            {
                return query.OrderBy(defaultOrderBy);
            }

            var sortFields = orderByParameter.SortBy.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            var orderByParts = new List<string>();
            foreach (var field in sortFields)
            {
                var (sortField, isDescending) = ParseSortField(field);

                var mapping = mappings.First(m =>
                    m.SortField.Equals(sortField, StringComparison.OrdinalIgnoreCase));

                var direction = (isDescending, mapping.Reverse) switch
                {
                    (false, false) => "ASC",
                    (false, true) => "DESC",
                    (true, false) => "DESC",
                    (true, true) => "ASC"
                };

                orderByParts.Add($"{mapping.PropertyName} {direction}");
            }

            var orderBy = string.Join(",", orderByParts);

            return query.OrderBy(orderBy);
        }
    }
    
    private static (string SortField, bool IsDescending) ParseSortField(string field)
    {
        var parts = field.Split(' ');
        var sortField = parts[0];
        var isDescending = parts.Length > 1 &&
                           parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortField, isDescending);
    }
}