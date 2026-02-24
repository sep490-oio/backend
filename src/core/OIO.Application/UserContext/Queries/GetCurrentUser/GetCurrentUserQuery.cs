using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Repositories;

namespace OIO.Application.UserContext.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<UserDto>;