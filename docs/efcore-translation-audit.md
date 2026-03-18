# EF Core Translation Audit

## Scope

Pass nay tap trung vao 2 nhom van de:

- Hard / likely translation failures: query trong `IQueryable` truy cap member tren property dang duoc map bang `HasConversion(...)`, dac biet la strong-ID `.Value`.
- Soft EF query smells: projection goi `ToDto()`, mapper methods, hoac domain methods sau khi van con nam trong LINQ-to-Entities path.

Source of truth de phan loai:

- `src/infrastructure/OIO.Infrastructure/Persistence/ApplicationDbContext.cs`
- `src/infrastructure/OIO.Infrastructure/Persistence/Configurations/**`

Rule chot:

- Strong-ID / value object duoc map bang `HasConversion(...)`: so sanh tren chinh property, vi du `x.UserId == UserId.From(guid)`.
- Khong query `x.Id.Value`, `x.ForeignId.Value`, `ids.Contains(x.Id.Value)` trong `IQueryable`.
- `ComplexProperty` member nhu `Status.Id`, `Title.Value`, `Pricing.CurrentAmount` thuong an toan hon, nhung van can can than neu projection co domain methods.
- `ToDto()` / domain methods trong `Select(...)` chi nen dung sau khi da materialize (`ToListAsync`, paged materialization, v.v.).

## Hard / likely failures da fix

### Warehouse

- `src/core/OIO.Application/Context/WarehouseContext/Queries/GetWarehouseItems/GetWarehouseItemsQuery.cs`
  - Doi `StorageLocationId` / `InboundShipmentId` sang so sanh bang value object.
  - Trang thai warehouse item parse thanh `WarehouseItemStatus` roi moi filter.
  - Mapping `ToDto()` day ve sau khi da materialize page.
- `src/core/OIO.Application/Context/WarehouseContext/Queries/GetInboundShipments/GetInboundShipmentsQuery.cs`
  - Doi `SellerId` sang `UserId`.
  - Parse `InboundShipmentStatus` truoc khi filter.
  - Materialize roi moi `ToDto()`.
- `src/core/OIO.Application/Context/WarehouseContext/Queries/GetOutboundShipments/GetOutboundShipmentsQuery.cs`
  - Doi `OrderId` sang `OrderId`.
  - Parse `OutboundShipmentStatus`.
  - Materialize roi moi `ToDto()`.
- `src/core/OIO.Application/Context/WarehouseContext/Queries/GetInspectionQueue/GetInspectionQueueQuery.cs`
  - Doi `Contains(x.Id.Value)` sang `Contains(x.Id)` bang `ItemId`.
  - Chuyen lookup item sang dictionary in-memory.
- `src/core/OIO.Application/Context/WarehouseContext/Queries/GetStorageLocations/GetStorageLocationsQuery.cs`
  - Bo mapper method trong `IQueryable`, map sau khi materialize.

### Auction

- `src/core/OIO.Application/Context/AuctionContext/Queries/GetAuctions/GetAuctionsQueryHandler.cs`
  - Bo projection goi `RemainingTime(...)` / `IsEndingSoon(...)` tren SQL path.
  - Include `Item.Media` va `BuyNowReservations`, sau do map `AuctionListItemDto` in-memory.
- `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyAuctions/GetMyAuctionsQueryHandler.cs`
  - Chuyen sang cung pattern materialize-then-map nhu `GetAuctions`.
- `src/core/OIO.Application/Context/AuctionContext/Queries/GetPublicSellerItems/GetPublicSellerItemsQueryHandler.cs`
  - Query item page truoc, lay `itemIds` dang strong-ID.
  - Query auctions bang `itemIds.Contains(x.ItemId)`.
  - Projection `PublicSellerItemDto` va media mapping chuyen sang in-memory.
- `src/core/OIO.Application/Context/AuctionContext/Queries/GetReviewQueue/GetReviewQueueQuery.cs`
  - Materialize page `Item` voi `Auctions` va `Media`, sau do map `ReviewQueueItemDto` in-memory.
- `src/core/OIO.Application/Context/AuctionContext/Queries/GetCategoryChildren/GetCategoryChildrenQueryHandler.cs`
  - Bo `ToDto()` method call khoi SQL path, map sau paging.

### Moderation

- `src/core/OIO.Application/Context/ModerationContext/Queries/GetDisputeThread/GetDisputeThreadQuery.cs`
  - `participantIds` giu dang `UserId`, khong convert thanh `Guid` roi query `x.Id.Value`.
- `src/core/OIO.Application/Context/ModerationContext/Queries/GetDisputeMessages/GetDisputeMessagesQuery.cs`
  - `senderIds` giu dang `UserId`.
  - Query users bang `senderIds.Contains(x.Id)`.
  - Sorting bang `x.Id` tren SQL path; phan sort `x.Id.Value` con lai la in-memory sau materialization.

### Payment

- `src/core/OIO.Application/Context/PaymentContext/Queries/Admins/GetAdminWithdrawals/GetAdminWithdrawalsQuery.cs`
  - Doi filter `UserId` sang `UserId`.
- `src/core/OIO.Application/Context/PaymentContext/Queries/GetMyWalletTransactionById/GetMyWalletTransactionByIdQuery.cs`
  - Doi `x.Id.Value == request.TransactionId` sang `x.Id == WalletTransactionId.From(...)`.
- `src/core/OIO.Application/Context/PaymentContext/Queries/Admins/GetAdminWithdrawalById/GetAdminWithdrawalByIdQuery.cs`
  - Doi sang `WithdrawalRequestId`.
- `src/core/OIO.Application/Context/PaymentContext/Queries/Admins/GetAdminTransactionById/GetAdminTransactionByIdQuery.cs`
  - Doi sang `TransactionId`.
- `src/core/OIO.Application/Context/PaymentContext/Queries/Admins/GetAdminEscrowById/GetAdminEscrowByIdQuery.cs`
  - Doi sang `EscrowId`.

### User

- `src/core/OIO.Application/Context/UserContext/Queries/GetAllTermsDocuments/GetAllTermsDocumentsQueryHandler.cs`
  - Bo top-level DTO projection khoi SQL path, map bang `TermsMappings.ToDto()` sau materialization.
- `src/core/OIO.Application/Context/UserContext/Queries/GetActiveTerms/GetActiveTermsQueryHandler.cs`
  - Cung pattern materialize-then-map cho `TermsDocumentDto`.
- `src/core/OIO.Application/Context/UserContext/Queries/GetActiveTermsByType/GetActiveTermsByTypeQueryHandler.cs`
  - Lay `TermsDocument` entity truoc, sau do map in-memory.
- `src/core/OIO.Application/Context/UserContext/Queries/GetMyAcceptedTerms/GetMyAcceptedTermsQueryHandler.cs`
  - Bo nested `TermsAcceptanceDto` / `TermsDocumentDto` projection khoi SQL path, map bang `TermsMappings`.

### Command handlers co nguy co translation failure da fix

- `src/core/OIO.Application/Context/PaymentContext/Commands/Withdrawals/CancelWithdrawalRequestCommand.cs`
- `src/core/OIO.Application/Context/PaymentContext/Commands/Withdrawals/ProcessWithdrawalCommands.cs`
- `src/core/OIO.Application/Context/PaymentContext/Commands/PaymentMethods/DeletePaymentMethodCommand.cs`
- `src/core/OIO.Application/Context/PaymentContext/Commands/PaymentMethods/SetDefaultPaymentMethodCommand.cs`
- `src/core/OIO.Application/Context/PaymentContext/Commands/Escrows/ReleaseEscrow/ReleaseEscrowCommand.cs`
- `src/core/OIO.Application/Context/PaymentContext/Commands/Escrows/ReleaseEscrowToSellerCommand.cs`
- `src/core/OIO.Application/Context/PaymentContext/Commands/Escrows/RefundEscrow/RefundEscrowCommand.cs`
- `src/core/OIO.Application/Context/PaymentContext/Commands/Refunds/RefundFromEscrowCommand.cs`
- `src/core/OIO.Application/Context/AuctionContext/Commands/ReturnAuctionDeposit/ReturnAuctionDepositCommand.cs`
- `src/core/OIO.Application/Context/AuctionContext/Commands/ForfeitAuctionDeposit/ForfeitAuctionDepositCommand.cs`

Tat ca cac file tren da duoc chuyen tu pattern `x.Id.Value == requestGuid` sang `x.Id == StrongId.From(requestGuid)`.

## Soft smells con lai

Nhung diem duoi day khong phai hard failure vua thay, nhung nen don dep trong phase tiep theo:

- `src/core/OIO.Application/Context/ModerationContext/Queries/GetAccessibleDisputes/GetAccessibleDisputesQuery.cs`
  - Dang gom `DisputeMessage` va shape summary in-memory sau khi materialize visible messages.
  - Khong phai translation risk ro rang, nhung con co the toi uu tiep neu can giam memory.
- `src/core/OIO.Application/Context/AuctionContext/Queries/GetAuctionById/GetAuctionByIdQueryHandler.cs`
  - Dang shape cac collection DTO trong memory sau eager loading; hop ly cho detail query, nhung van la diem can theo doi neu entity graph lon hon.

## Query patterns duoc xem la on trong pass nay

- Compare scalar that su dung true scalar property, vi du warehouse `ItemId` dang la `Guid`.
- Filter tren `ComplexProperty` members nhu:
  - `Status.Id`
  - `Title.Value`
  - `Pricing.CurrentAmount`

## Remediation checklist cho query moi

1. Neu property map bang `HasConversion(...)`, khong chui vao `.Value` trong `Where`, `OrderBy`, `Contains`, `Join`.
2. Neu can `Contains`, giu ca hai phia cung mot kieu strong-ID.
3. Neu projection can `ToDto()` hoac goi domain methods, can nhac:
   - page / filter tren SQL truoc
   - materialize
   - map in-memory sau
4. Neu query co nhieu `Include` + collection projections, uu tien `AsNoTracking()` va `AsSplitQuery()` cho list/read-only path khi hop ly.
