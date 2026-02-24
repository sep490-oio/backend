using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;

namespace OIO.Application.UserContext.Queries.GetUserAddresses;

public sealed record GetUserAddressesQuery(PagedParameters PagedParameters) : IQuery<PagedResult<UserAddressDto>>;