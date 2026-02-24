using CSharpFunctionalExtensions;
using MediatR;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, UnitResult<Error>>
    where TCommand : ICommand { }

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse, Error>>
    where TCommand : ICommand<TResponse> { }