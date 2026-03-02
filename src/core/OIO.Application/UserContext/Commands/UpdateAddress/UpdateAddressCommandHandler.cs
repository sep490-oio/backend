using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Application.UserContext.Mappings;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.UpdateAddress;

internal sealed class UpdateAddressCommandHandler
    : ICommandHandler<UpdateAddressCommand, UserAddressDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    

    public UpdateAddressCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        
    }

    public async Task<Result<UserAddressDto, Error>> Handle(
        UpdateAddressCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc =  _clock.UtcNow;
        

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            _currentUser.UserId,
            query =>  query
                .Include(x => x.Addresses),
            cancellationToken);
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        var addressId = UserAddressId.From(request.AddressId);
        
        var existing = user.Addresses.FirstOrDefault(a => a.Id == addressId);
        
        if (existing is null)
            return UserErrors.User.AddressNotFound(addressId);
        
        Address? address = null;
        if (request.Street is not null || request.Ward is not null ||
            request.District is not null || request.City is not null ||
            request.PostalCode is not null)
        {
            var addressR = Address.Create(
                request.Street ?? existing.Address.Street,
                request.Ward ?? existing.Address.Ward,
                request.District ?? existing.Address.District,
                request.City ?? existing.Address.City,
                request.PostalCode ?? existing.Address.PostalCode);

            if (addressR.IsFailure)
            {
                return addressR.Error;
            }
            
            address = addressR.Value;
        }
        
        PhoneNumber? phoneNumber = null;
        if (request.PhoneNumber is not null && request.PhoneNumber != existing.PhoneNumber.Value)
        {
            var phoneNumberR = PhoneNumber.Create(request.PhoneNumber, request.CountryCode);

            if (phoneNumberR.IsFailure)
            {
                return phoneNumberR.Error;
            }
            
            phoneNumber = phoneNumberR.Value;
        }
        
        var addressType = AddressType.FromId(request.Type).GetValueOrDefault(existing.Type);
        

        var updateR = user.UpdateAddress(
            addressId,
            now: nowUtc,
            type: addressType,
            recipientName: request.RecipientName,
            phoneNumber: phoneNumber,
            address: address);

        if (updateR.IsFailure)
            return updateR.Error;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = user.Addresses.First(a => a.Id == addressId);
        return updated.ToDto();
    }
}