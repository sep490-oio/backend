using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<UserDto>;