using CSharpFunctionalExtensions;
using MediatR;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse, Error>>;
