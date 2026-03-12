using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.ShippingContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Infrastructure.Persistence.Converters;

namespace OIO.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IDbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.AddQuartz(builder => builder.UsePostgreSql());
    }

    public new DbSet<TEntity> Set<TEntity>() where TEntity : class, IEntity
    {
        return base.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync<TEntity, TId>(
        TId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryBuilder = null,
        CancellationToken cancellationToken = default)
        where TId : IEntityId, new()
        where TEntity : class, IEntity<TId>
    {
        IQueryable<TEntity> query = Set<TEntity>();

        if (queryBuilder is not null)
            query = queryBuilder(query);
        
        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public void Insert<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        Set<TEntity>().Add(entity);
    }

    public void InsertRange<TEntity>(IReadOnlyCollection<TEntity> entities) where TEntity : class, IEntity
    {
        Set<TEntity>().AddRange(entities);
    }
    
    public new void Update<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        Set<TEntity>().Update(entity);
    }

    public new void Remove<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        Set<TEntity>().Remove(entity);
    }

    public Task<int> ExecuteSqlAsync(string sql, IEnumerable<SqlParameter> parameters, CancellationToken cancellationToken = default)
    {
        return Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<AuctionDepositId>()
            .HaveConversion<EfCoreConverters.AuctionDepositIdEfCoreValueConverter>();

        configurationBuilder.Properties<AuctionEmergencyActionId>()
            .HaveConversion<EfCoreConverters.AuctionEmergencyActionIdEfCoreValueConverter>();

        configurationBuilder.Properties<AuctionId>()
            .HaveConversion<EfCoreConverters.AuctionIdEfCoreValueConverter>();

        configurationBuilder.Properties<AuctionParticipantId>()
            .HaveConversion<EfCoreConverters.AuctionParticipantIdEfCoreValueConverter>();

        configurationBuilder.Properties<AuctionPriceHistoryId>()
            .HaveConversion<EfCoreConverters.AuctionPriceHistoryIdEfCoreValueConverter>();

        configurationBuilder.Properties<AuctionRelistHistoryId>()
            .HaveConversion<EfCoreConverters.AuctionRelistHistoryIdEfCoreValueConverter>();

        configurationBuilder.Properties<AuctionWatcherId>()
            .HaveConversion<EfCoreConverters.AuctionWatcherIdEfCoreValueConverter>();

        configurationBuilder.Properties<AuctionWinnerOfferId>()
            .HaveConversion<EfCoreConverters.AuctionWinnerOfferIdEfCoreValueConverter>();

        configurationBuilder.Properties<AutoBidId>()
            .HaveConversion<EfCoreConverters.AutoBidIdEfCoreValueConverter>();

        configurationBuilder.Properties<BidEventId>()
            .HaveConversion<EfCoreConverters.BidEventIdEfCoreValueConverter>();

        configurationBuilder.Properties<BidId>()
            .HaveConversion<EfCoreConverters.BidIdEfCoreValueConverter>();

        configurationBuilder.Properties<SealedBidId>()
            .HaveConversion<EfCoreConverters.SealedBidIdEfCoreValueConverter>();

        configurationBuilder.Properties<CategoryId>()
            .HaveConversion<EfCoreConverters.CategoryIdEfCoreValueConverter>();

        configurationBuilder.Properties<ItemId>()
            .HaveConversion<EfCoreConverters.ItemIdEfCoreValueConverter>();

        configurationBuilder.Properties<ItemMediaId>()
            .HaveConversion<EfCoreConverters.ItemMediaIdEfCoreValueConverter>();

        configurationBuilder.Properties<ItemModerationReviewId>()
            .HaveConversion<EfCoreConverters.ItemModerationReviewIdEfCoreValueConverter>();

        configurationBuilder.Properties<ItemQuestionId>()
            .HaveConversion<EfCoreConverters.ItemQuestionIdEfCoreValueConverter>();

        configurationBuilder.Properties<AdminReviewTaskId>()
            .HaveConversion<EfCoreConverters.AdminReviewTaskIdEfCoreValueConverter>();

        configurationBuilder.Properties<AuditLogId>()
            .HaveConversion<EfCoreConverters.AuditLogIdEfCoreValueConverter>();

        configurationBuilder.Properties<DisputeEvidenceId>()
            .HaveConversion<EfCoreConverters.DisputeEvidenceIdEfCoreValueConverter>();

        configurationBuilder.Properties<DisputeId>()
            .HaveConversion<EfCoreConverters.DisputeIdEfCoreValueConverter>();

        configurationBuilder.Properties<DisputeMessageId>()
            .HaveConversion<EfCoreConverters.DisputeMessageIdEfCoreValueConverter>();

        configurationBuilder.Properties<DisputeRefundId>()
            .HaveConversion<EfCoreConverters.DisputeRefundIdEfCoreValueConverter>();

        configurationBuilder.Properties<DisputeResponseTemplateId>()
            .HaveConversion<EfCoreConverters.DisputeResponseTemplateIdEfCoreValueConverter>();

        configurationBuilder.Properties<DisputeStatusHistoryId>()
            .HaveConversion<EfCoreConverters.DisputeStatusHistoryIdEfCoreValueConverter>();

        configurationBuilder.Properties<MonitoringAlertId>()
            .HaveConversion<EfCoreConverters.MonitoringAlertIdEfCoreValueConverter>();

        configurationBuilder.Properties<ReportId>()
            .HaveConversion<EfCoreConverters.ReportIdEfCoreValueConverter>();

        configurationBuilder.Properties<ReviewQueueId>()
            .HaveConversion<EfCoreConverters.ReviewQueueIdEfCoreValueConverter>();

        configurationBuilder.Properties<NotificationDeliveryId>()
            .HaveConversion<EfCoreConverters.NotificationDeliveryIdEfCoreValueConverter>();

        configurationBuilder.Properties<NotificationId>()
            .HaveConversion<EfCoreConverters.NotificationIdEfCoreValueConverter>();

        configurationBuilder.Properties<OrderId>()
            .HaveConversion<EfCoreConverters.OrderIdEfCoreValueConverter>();

        configurationBuilder.Properties<OrderReturnId>()
            .HaveConversion<EfCoreConverters.OrderReturnIdEfCoreValueConverter>();

        configurationBuilder.Properties<EscrowId>()
            .HaveConversion<EfCoreConverters.EscrowIdEfCoreValueConverter>();

        configurationBuilder.Properties<EscrowReleaseEventId>()
            .HaveConversion<EfCoreConverters.EscrowReleaseEventIdEfCoreValueConverter>();

        configurationBuilder.Properties<InvoiceId>()
            .HaveConversion<EfCoreConverters.InvoiceIdEfCoreValueConverter>();

        configurationBuilder.Properties<PaymentMethodId>()
            .HaveConversion<EfCoreConverters.PaymentMethodIdEfCoreValueConverter>();

        configurationBuilder.Properties<TransactionId>()
            .HaveConversion<EfCoreConverters.TransactionIdEfCoreValueConverter>();

        configurationBuilder.Properties<WalletId>()
            .HaveConversion<EfCoreConverters.WalletIdEfCoreValueConverter>();

        configurationBuilder.Properties<WalletTransactionId>()
            .HaveConversion<EfCoreConverters.WalletTransactionIdEfCoreValueConverter>();

        configurationBuilder.Properties<WithdrawalRequestId>()
            .HaveConversion<EfCoreConverters.WithdrawalRequestIdEfCoreValueConverter>();

        configurationBuilder.Properties<BuyerReviewId>()
            .HaveConversion<EfCoreConverters.BuyerReviewIdEfCoreValueConverter>();

        configurationBuilder.Properties<ReviewImageId>()
            .HaveConversion<EfCoreConverters.ReviewImageIdEfCoreValueConverter>();

        configurationBuilder.Properties<ReviewReportId>()
            .HaveConversion<EfCoreConverters.ReviewReportIdEfCoreValueConverter>();

        configurationBuilder.Properties<ReviewVoteId>()
            .HaveConversion<EfCoreConverters.ReviewVoteIdEfCoreValueConverter>();

        configurationBuilder.Properties<SellerRatingSummaryId>()
            .HaveConversion<EfCoreConverters.SellerRatingSummaryIdEfCoreValueConverter>();

        configurationBuilder.Properties<SellerReviewId>()
            .HaveConversion<EfCoreConverters.SellerReviewIdEfCoreValueConverter>();

        configurationBuilder.Properties<MediaUploadId>()
            .HaveConversion<EfCoreConverters.MediaUploadIdEfCoreValueConverter>();

        configurationBuilder.Properties<InboundShipmentId>()
            .HaveConversion<EfCoreConverters.InboundShipmentIdEfCoreValueConverter>();

        configurationBuilder.Properties<OutboundShipmentId>()
            .HaveConversion<EfCoreConverters.OutboundShipmentIdEfCoreValueConverter>();

        configurationBuilder.Properties<ShipmentEventId>()
            .HaveConversion<EfCoreConverters.ShipmentEventIdEfCoreValueConverter>();

        configurationBuilder.Properties<ShipmentTrackingEventId>()
            .HaveConversion<EfCoreConverters.ShipmentTrackingEventIdEfCoreValueConverter>();

        configurationBuilder.Properties<ShippingProviderConfigId>()
            .HaveConversion<EfCoreConverters.ShippingProviderConfigIdEfCoreValueConverter>();

        configurationBuilder.Properties<WarehouseItemId>()
            .HaveConversion<EfCoreConverters.WarehouseItemIdEfCoreValueConverter>();

        configurationBuilder.Properties<WarehouseStorageLocationId>()
            .HaveConversion<EfCoreConverters.WarehouseStorageLocationIdEfCoreValueConverter>();

        configurationBuilder.Properties<IdentityVerificationId>()
            .HaveConversion<EfCoreConverters.IdentityVerificationIdEfCoreValueConverter>();

        configurationBuilder.Properties<SellerKycDocumentId>()
            .HaveConversion<EfCoreConverters.SellerKycDocumentIdEfCoreValueConverter>();

        configurationBuilder.Properties<SellerKycHistoryId>()
            .HaveConversion<EfCoreConverters.SellerKycHistoryIdEfCoreValueConverter>();

        configurationBuilder.Properties<SellerKycId>()
            .HaveConversion<EfCoreConverters.SellerKycIdEfCoreValueConverter>();

        configurationBuilder.Properties<SellerProfileId>()
            .HaveConversion<EfCoreConverters.SellerProfileIdEfCoreValueConverter>();

        configurationBuilder.Properties<TermsAcceptanceId>()
            .HaveConversion<EfCoreConverters.TermsAcceptanceIdEfCoreValueConverter>();

        configurationBuilder.Properties<TermsDocumentId>()
            .HaveConversion<EfCoreConverters.TermsDocumentIdEfCoreValueConverter>();

        configurationBuilder.Properties<UserAddressId>()
            .HaveConversion<EfCoreConverters.UserAddressIdEfCoreValueConverter>();

        configurationBuilder.Properties<UserId>()
            .HaveConversion<EfCoreConverters.UserIdEfCoreValueConverter>();

        configurationBuilder.Properties<UserLoginHistoryId>()
            .HaveConversion<EfCoreConverters.UserLoginHistoryIdEfCoreValueConverter>();

        configurationBuilder.Properties<UserNotificationPreferenceId>()
            .HaveConversion<EfCoreConverters.UserNotificationPreferenceIdEfCoreValueConverter>();

        configurationBuilder.Properties<UserRefreshTokenId>()
            .HaveConversion<EfCoreConverters.UserRefreshTokenIdEfCoreValueConverter>();

        configurationBuilder.Properties<UserRiskFlagId>()
            .HaveConversion<EfCoreConverters.UserRiskFlagIdEfCoreValueConverter>();

        configurationBuilder.Properties<UserSessionId>()
            .HaveConversion<EfCoreConverters.UserSessionIdEfCoreValueConverter>();

        configurationBuilder.Properties<VerificationDocumentId>()
            .HaveConversion<EfCoreConverters.VerificationDocumentIdEfCoreValueConverter>();

        configurationBuilder.Properties<VerificationHistoryId>()
            .HaveConversion<EfCoreConverters.VerificationHistoryIdEfCoreValueConverter>();
    }
}