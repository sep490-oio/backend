using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Sorting;

public sealed class SortMappingDefinition : ISortMappingDefinition
{
    public required SortMapping[] Mappings { get; init; }

    public UnitResult<Error> ValidateMappings(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return UnitResult.Success<Error>();
        }

        var sortFields = sort
            .Split(',')
            .Select(f => f.Trim().Split(' ')[0])
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        var result = sortFields.All(f => Mappings.Any(m => m.SortField.Equals(f, StringComparison.OrdinalIgnoreCase)));

        return result
            ? UnitResult.Success<Error>()
            : Error.Validation("SortBy", "SortBy.Invalid",
                $"The provided sort parameter isn't valid: '{sort}', valid fields are: {string.Join(", ", Mappings.Select(m => m.SortField))}");
    }
}