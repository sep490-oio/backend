using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.CreatePaymentDispute;

public sealed record CreatePaymentDisputeCommand(
    Guid PaymentId,
    string Domain,
    string CaseType,
    string Title,
    string Description) : ICommand<DisputeIntakeDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CreatePaymentDisputeCommand.Check()
            .WithOwnerName("CreatePaymentDispute")
            .Field(PaymentId).NotEmptyGuid()
            .Field(Domain).NotEmpty()
            .Field(CaseType).NotEmpty()
            .Field(Title).NotEmpty()
            .Field(Description).NotEmpty();
}

internal sealed class CreatePaymentDisputeCommandHandler(
    IDbContext dbContext,
    ICurrentUser currentUser,
    IDisputeIntakeService intakeService)
    : ICommandHandler<CreatePaymentDisputeCommand, DisputeIntakeDto>
{
    public async Task<Result<DisputeIntakeDto, Error>> Handle(
        CreatePaymentDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Set<Transaction>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == TransactionId.From(request.PaymentId), cancellationToken);

        if (transaction is null)
            return Error.NotFound("Payment.NotFound", "Payment transaction was not found.");

        var userId = currentUser.UserId;
        if (userId != transaction.UserId)
            return Error.Forbidden("Payment.NotOwner", "You do not own this payment transaction.");

        var snapshot = DisputeContextSnapshotBuilder.ForTransaction(transaction);

        return await intakeService.CreateDisputeAsync(new CreateDisputeRequest(
            Domain: request.Domain,
            CaseType: request.CaseType,
            PrimaryTargetType: "payment",
            OrderId: transaction.OrderId?.Value,
            AuctionId: transaction.AuctionId?.Value,
            ShipmentId: null,
            WarehouseItemId: null,
            PaymentId: request.PaymentId,
            ComplainantUserId: userId.Value,
            RespondentUserId: null,
            Title: request.Title,
            Description: request.Description,
            ContextSnapshotJson: snapshot), cancellationToken);
    }
}
