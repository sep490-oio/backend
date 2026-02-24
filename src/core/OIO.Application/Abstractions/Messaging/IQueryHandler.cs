using CSharpFunctionalExtensions;
using MediatR;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse, Error>>
    where TQuery : IQuery<TResponse> { }