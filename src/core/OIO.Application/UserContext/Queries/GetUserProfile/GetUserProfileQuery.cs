using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;

namespace OIO.Application.UserContext.Queries.GetUserProfile;

public sealed record GetUserProfileQuery : IQuery<UserProfileDto>;