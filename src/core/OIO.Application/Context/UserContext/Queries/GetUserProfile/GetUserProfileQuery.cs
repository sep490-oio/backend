using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetUserProfile;

public sealed record GetUserProfileQuery : IQuery<UserProfileDto>;