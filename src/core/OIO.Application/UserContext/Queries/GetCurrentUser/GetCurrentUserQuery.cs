using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;


namespace OIO.Application.UserContext.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<UserDto>;