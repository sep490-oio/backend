using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetUserAddresses;

public sealed record GetUserAddressesQuery(PagedParameters PagedParameters) : IQuery<PagedList<UserAddressDto>>;