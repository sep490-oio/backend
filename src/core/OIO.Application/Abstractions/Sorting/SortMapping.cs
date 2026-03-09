using System.Linq.Expressions;
using OIO.Domain.SeedWork.Utils;

namespace OIO.Application.Abstractions.Sorting;

public sealed record SortMapping(string SortField, string PropertyName, bool Reverse = false);

public class SortMappingBuilder<TSource, TDestination>
{
    private readonly List<SortMapping> _sortMappings;
    
    public SortMappingBuilder()
    {
        _sortMappings = [];
    }
    
    public SortMappingBuilder<TSource, TDestination> Map(
        Expression<Func<TSource, object?>>  sortField,
        Expression<Func<TDestination, object?>> propertyName, 
        bool reverse = false)
    {
        _sortMappings.Add(new SortMapping(sortField.GetOrAddName(), propertyName.GetOrAddName(), reverse));
        return this;
    }
    
    
    
    public static SortMappingBuilder<TSource, TDestination> Create() => new();

    public SortMappingDefinition Build() => new SortMappingDefinition()
    {
        Mappings = _sortMappings.ToArray()
    };
}