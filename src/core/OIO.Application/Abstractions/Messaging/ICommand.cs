using CSharpFunctionalExtensions;
using MediatR;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Messaging;

public interface IHasValidate
{
    ViolationsError Validate();
}

public interface IBaseCommand;

public interface ICommand : IRequest<UnitResult<Error>>, IBaseCommand { }

public interface ICommand<TResponse> : IRequest<Result<TResponse, Error>>, IBaseCommand{ }