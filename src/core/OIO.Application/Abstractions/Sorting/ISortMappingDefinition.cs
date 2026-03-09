using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Sorting;

public interface ISortMappingDefinition
{
    SortMapping[] Mappings { get; }

    UnitResult<Error> ValidateMappings(string? sort);
}