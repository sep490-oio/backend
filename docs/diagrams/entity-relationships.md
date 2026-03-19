# Entity Relationship Diagrams

## Full Domain ER Diagram

```mermaid
erDiagram
    %% ============================================================
    %% UserContext
    %% ============================================================
    User {
        uuid Id PK
        string Email
        string PasswordHash
        string PhoneNumber
        UserStatus Status
        bool TwoFactorEnabled
        string TotpSecretKey
        datetime CreatedAt
        datetime ModifiedAt
    }
    UserProfile {
        uuid Id PK
        uuid UserId FK
        string FirstName
        string LastName
        string DisplayName
        string AvatarUrl
        Gender Gender
        date DateOfBirth
    }
    SellerProfile {
        uuid Id PK
        uuid UserId FK
        string BusinessName
        string TaxId
        SellerProfileStatus Status
        decimal TrustScore
        datetime VerifiedAt
    }
    UserAddress {
        uuid Id PK
        uuid UserId FK
        AddressType Type
        string Street
        string Ward
        string District
        string Province
        bool IsDefault
    }
    UserSession {
        uuid Id PK
        uuid UserId FK
        string DeviceInfo
        string IpAddress
        datetime ExpiresAt
        datetime CreatedAt
    }
    UserRefreshToken {
        uuid Id PK
        uuid UserId FK
        string TokenHash
        datetime ExpiresAt
        bool IsRevoked
    }
    IdentityVerification {
        uuid Id PK
        uuid UserId FK
        IdentityVerificationStatus Status
        VerificationType Type
        datetime SubmittedAt
        datetime ReviewedAt
    }
    VerificationDocument {
        uuid Id PK
        uuid VerificationId FK
        VerificationDocumentType DocType
        string StorageUrl
    }
    UserNotificationPreference {
        uuid Id PK
        uuid UserId FK
        string Channel
        bool Enabled
    }
    UserRiskFlag {
        uuid Id PK
        uuid UserId FK
        RiskFlagSeverity Severity
        string Reason
        datetime CreatedAt
    }
    UserLoginHistory {
        uuid Id PK
        uuid UserId FK
        LoginStatus Status
        string IpAddress
        datetime AttemptedAt
    }
    RecoveryCode {
        uuid Id PK
        uuid UserId FK
        string CodeHash
        bool IsUsed
    }
    Role {
        uuid Id PK
        string Name
    }
    Permission {
        uuid Id PK
        string Name
    }
    UserRole {
        uuid UserId FK
        uuid RoleId FK
    }
    RolePermission {
        uuid RoleId FK
        uuid PermissionId FK
    }
    TermsDocument {
        uuid Id PK
        string Version
        string Type
        bool IsActive
    }
    TermsAcceptance {
        uuid Id PK
        uuid UserId FK
        uuid TermsDocumentId FK
        datetime AcceptedAt
    }

    User ||--o| UserProfile : has
    User ||--o| SellerProfile : has
    User ||--o{ UserAddress : has
    User ||--o{ UserSession : has
    User ||--o{ UserRefreshToken : has
    User ||--o{ IdentityVerification : submits
    User ||--o{ UserNotificationPreference : configures
    User ||--o{ UserRiskFlag : flagged
    User ||--o{ UserLoginHistory : logs
    User ||--o{ RecoveryCode : has
    User ||--o{ UserRole : assigned
    User ||--o{ TermsAcceptance : accepts
    IdentityVerification ||--o{ VerificationDocument : contains
    Role ||--o{ UserRole : assigned
    Role ||--o{ RolePermission : grants
    Permission ||--o{ RolePermission : granted
    TermsDocument ||--o{ TermsAcceptance : accepted

    %% ============================================================
    %% CatalogContext
    %% ============================================================
    Category {
        uuid Id PK
        uuid ParentId FK
        string Name
        string Slug
        int SortOrder
    }
    Item {
        uuid Id PK
        uuid SellerId FK
        uuid CategoryId FK
        string Title
        string Description
        ItemCondition Condition
        ItemStatus Status
        datetime CreatedAt
    }
    ItemMedia {
        uuid Id PK
        uuid ItemId FK
        string PublicId
        string Url
        bool IsPrimary
        int SortOrder
    }
    ItemModerationReview {
        uuid Id PK
        uuid ItemId FK
        uuid ReviewerId FK
        ModerationAction Action
        string Reason
        datetime ReviewedAt
    }
    ItemQuestion {
        uuid Id PK
        uuid ItemId FK
        uuid AskerId FK
        string Question
        string Answer
        datetime AskedAt
    }

    Category ||--o{ Category : children
    Category ||--o{ Item : contains
    User ||--o{ Item : sells
    Item ||--o{ ItemMedia : has
    Item ||--o{ ItemModerationReview : reviewed
    Item ||--o{ ItemQuestion : has

    %% ============================================================
    %% AuctionContext
    %% ============================================================
    Auction {
        uuid Id PK
        uuid ItemId FK
        AuctionType AuctionType
        AuctionStatus Status
        Money StartingPrice
        Money ReservePrice
        Money BuyNowPrice
        Money BidIncrement
        datetime StartTime
        datetime EndTime
        uuid WinnerId FK
        int BidCount
        int ViewCount
        bool IsFeatured
        datetime CreatedAt
    }
    Bid {
        uuid Id PK
        uuid AuctionId FK
        uuid BidderId FK
        Money Amount
        uuid AutoBidId FK
        bool IsAutoBid
        BidStatus Status
        string IpAddress
        datetime CreatedAt
    }
    AutoBid {
        uuid Id PK
        uuid AuctionId FK
        uuid BidderId FK
        Money MaxAmount
        Money IncrementAmount
        AutoBidStatus Status
        datetime CreatedAt
    }
    SealedBid {
        uuid Id PK
        uuid AuctionId FK
        uuid BidderId FK
        string EncryptedAmount
        SealedBidStatus Status
        datetime CreatedAt
    }
    AuctionDeposit {
        uuid Id PK
        uuid AuctionId FK
        uuid UserId FK
        Money Amount
        DepositStatus Status
        datetime CreatedAt
    }
    AuctionParticipant {
        uuid Id PK
        uuid AuctionId FK
        uuid UserId FK
        ParticipantJoinStatus JoinStatus
        ParticipantQualificationStatus QualificationStatus
        datetime JoinedAt
    }
    AuctionWatcher {
        uuid Id PK
        uuid AuctionId FK
        uuid UserId FK
        bool NotifyOnBid
        bool NotifyOnEnd
    }
    AuctionWinnerOffer {
        uuid Id PK
        uuid AuctionId FK
        uuid UserId FK
        WinnerOfferStatus Status
        Money OfferPrice
        datetime ExpiresAt
    }
    AuctionBuyNowReservation {
        uuid Id PK
        uuid AuctionId FK
        uuid BuyerId FK
        BuyNowReservationStatus Status
        datetime ExpiresAt
    }
    AuctionPriceHistory {
        uuid Id PK
        uuid AuctionId FK
        AuctionPriceHistoryType Type
        Money Price
        datetime RecordedAt
    }
    AuctionRelistHistory {
        uuid Id PK
        uuid AuctionId FK
        string Reason
        datetime RelistedAt
    }
    AuctionEmergency {
        uuid Id PK
        uuid AuctionId FK
        EmergencyStatus Status
        string Reason
    }
    BidEvent {
        uuid Id PK
        uuid BidId FK
        string EventType
        datetime OccurredAt
    }

    Item ||--o| Auction : "listed as"
    Auction ||--o{ Bid : receives
    Auction ||--o{ AutoBid : has
    Auction ||--o{ SealedBid : has
    Auction ||--o{ AuctionDeposit : requires
    Auction ||--o{ AuctionParticipant : has
    Auction ||--o{ AuctionWatcher : watched
    Auction ||--o{ AuctionWinnerOffer : offers
    Auction ||--o{ AuctionBuyNowReservation : has
    Auction ||--o{ AuctionPriceHistory : tracks
    Auction ||--o{ AuctionRelistHistory : relisted
    Auction ||--o{ AuctionEmergency : emergencies
    User ||--o{ Bid : places
    User ||--o{ AutoBid : configures
    User ||--o{ AuctionParticipant : joins
    User ||--o{ AuctionWatcher : watches
    AutoBid ||--o{ Bid : triggers
    Bid ||--o{ BidEvent : logs

    %% ============================================================
    %% PaymentContext
    %% ============================================================
    Wallet {
        uuid Id PK
        uuid UserId FK
        WalletType Type
        Money Balance
        Money FrozenBalance
        string Currency
    }
    WalletTransaction {
        uuid Id PK
        uuid WalletId FK
        WalletTransactionType Type
        Money Amount
        Money BalanceAfter
        string ReferenceId
        datetime CreatedAt
    }
    Transaction {
        uuid Id PK
        uuid UserId FK
        TransactionType Type
        TransactionStatus Status
        Money Amount
        string Currency
        PaymentMethodType PaymentMethod
        string GatewayRef
        datetime CreatedAt
    }
    Escrow {
        uuid Id PK
        uuid OrderId FK
        uuid BuyerId FK
        uuid SellerId FK
        Money Amount
        EscrowStatus Status
        datetime CreatedAt
    }
    EscrowReleaseEvent {
        uuid Id PK
        uuid EscrowId FK
        EscrowReleaseType ReleaseType
        EscrowReleaseTo ReleaseTo
        Money Amount
        datetime ReleasedAt
    }
    PaymentMethod {
        uuid Id PK
        uuid UserId FK
        PaymentMethodType Type
        string Details
        bool IsDefault
    }
    WithdrawalRequest {
        uuid Id PK
        uuid UserId FK
        Money Amount
        WithdrawalStatus Status
        string BankAccount
        datetime CreatedAt
    }
    Invoice {
        uuid Id PK
        uuid TransactionId FK
        InvoiceStatus Status
        Money Amount
        datetime IssuedAt
    }
    GatewayWebhookEvent {
        uuid Id PK
        string Provider
        string EventType
        WebhookProcessingStatus Status
        string RawPayload
        datetime ReceivedAt
    }

    User ||--|| Wallet : owns
    Wallet ||--o{ WalletTransaction : records
    User ||--o{ Transaction : makes
    User ||--o{ PaymentMethod : registers
    User ||--o{ WithdrawalRequest : requests
    Escrow ||--o{ EscrowReleaseEvent : releases
    Transaction ||--o| Invoice : generates

    %% ============================================================
    %% OrderContext
    %% ============================================================
    Order {
        uuid Id PK
        string OrderNumber
        uuid AuctionId FK
        uuid BuyerId FK
        uuid SellerId FK
        OrderStatus Status
        Money ItemPrice
        Money ShippingFee
        Money PlatformFee
        Money TotalAmount
        datetime PaymentDueAt
        datetime PaidAt
        datetime ShippedAt
        datetime DeliveredAt
        datetime CompletedAt
        datetime CancelledAt
    }
    OrderReturn {
        uuid Id PK
        uuid OrderId FK
        uuid BuyerId FK
        OrderReturnStatus Status
        string ReasonCode
        string Description
        datetime RequestedAt
    }

    Auction ||--o| Order : "results in"
    User ||--o{ Order : "buys"
    User ||--o{ Order : "sells"
    Order ||--o| OrderReturn : "may have"
    Order ||--o{ Escrow : "secured by"

    %% ============================================================
    %% WarehouseContext
    %% ============================================================
    InboundShipment {
        uuid Id PK
        uuid ItemId FK
        uuid SellerId FK
        ShippingProviderCode Provider
        string CarrierTrackingNumber
        InboundShipmentStatus Status
        datetime ArrivedAt
        datetime CreatedAt
    }
    OutboundShipment {
        uuid Id PK
        uuid OrderId FK
        ShippingProviderCode Provider
        string CarrierTrackingNumber
        OutboundShipmentStatus Status
        datetime CreatedAt
    }
    WarehouseItem {
        uuid Id PK
        uuid ItemId FK
        uuid InboundShipmentId FK
        uuid StorageLocationId FK
        WarehouseItemStatus Status
        datetime ReceivedAt
        datetime CreatedAt
    }
    WarehouseInspection {
        uuid Id PK
        uuid WarehouseItemId FK
        uuid InboundShipmentId FK
        uuid ItemId FK
        ItemCondition DeclaredCondition
        WarehouseItemCondition ConditionOnArrival
        WarehouseInspectionDecisionStatus DecisionStatus
        uuid InspectedBy FK
        datetime InspectedAt
    }
    WarehouseStorageLocation {
        uuid Id PK
        string Label
        string Zone
        int Capacity
    }
    ShipmentTrackingEvent {
        uuid Id PK
        string ShipmentType
        uuid ShipmentId FK
        ShippingProviderCode Provider
        NormalizedTrackingStatus NormalizedStatus
        string CarrierStatusRaw
        datetime EventTime
    }
    ShippingProviderConfig {
        uuid Id PK
        ShippingProviderCode Code
        ShippingEnvironment Environment
        string ApiKey
    }

    Item ||--o{ InboundShipment : "shipped to warehouse"
    Order ||--o{ OutboundShipment : "shipped to buyer"
    InboundShipment ||--o| WarehouseItem : creates
    InboundShipment ||--o{ ShipmentTrackingEvent : tracks
    OutboundShipment ||--o{ ShipmentTrackingEvent : tracks
    WarehouseItem ||--o| WarehouseInspection : inspected
    WarehouseItem }o--o| WarehouseStorageLocation : "stored at"

    %% ============================================================
    %% ModerationContext
    %% ============================================================
    Report {
        uuid Id PK
        uuid ReporterId FK
        string TargetType
        uuid TargetId
        ReportStatus Status
        string Reason
        datetime CreatedAt
    }
    Dispute {
        uuid Id PK
        uuid OrderId FK
        uuid InitiatorId FK
        DisputeType Type
        DisputeStatus Status
        DisputePriority Priority
        DesiredResolution DesiredResolution
        datetime CreatedAt
    }
    DisputeMessage {
        uuid Id PK
        uuid DisputeId FK
        uuid SenderId FK
        string Content
        datetime SentAt
    }
    DisputeEvidence {
        uuid Id PK
        uuid DisputeId FK
        EvidenceType Type
        string Url
    }
    DisputeStatusHistory {
        uuid Id PK
        uuid DisputeId FK
        DisputeStatus FromStatus
        DisputeStatus ToStatus
        datetime ChangedAt
    }
    DisputeRefund {
        uuid Id PK
        uuid DisputeId FK
        RefundType Type
        Money Amount
    }
    MonitoringAlert {
        uuid Id PK
        AlertSeverity Severity
        AlertStatus Status
        string Message
        datetime CreatedAt
    }
    AdminReviewTask {
        uuid Id PK
        string TargetType
        uuid TargetId
        AdminReviewTaskStatus Status
        uuid AssigneeId FK
    }
    ReviewQueue {
        uuid Id PK
        string TargetType
        uuid TargetId
        ReviewQueueStatus Status
    }

    User ||--o{ Report : submits
    Order ||--o{ Dispute : "disputed via"
    Dispute ||--o{ DisputeMessage : messages
    Dispute ||--o{ DisputeEvidence : evidence
    Dispute ||--o{ DisputeStatusHistory : history
    Dispute ||--o{ DisputeRefund : refunds

    %% ============================================================
    %% NotificationContext
    %% ============================================================
    Notification {
        uuid Id PK
        uuid UserId FK
        string Type
        string Title
        string Body
        NotificationStatus Status
        NotificationPriority Priority
        datetime CreatedAt
    }
    NotificationDelivery {
        uuid Id PK
        uuid NotificationId FK
        NotificationChannel Channel
        NotificationDeliveryStatus Status
        datetime SentAt
    }

    User ||--o{ Notification : receives
    Notification ||--o{ NotificationDelivery : "delivered via"

    %% ============================================================
    %% ReviewContext
    %% ============================================================
    SellerReview {
        uuid Id PK
        uuid OrderId FK
        uuid BuyerId FK
        uuid SellerId FK
        int Rating
        string Comment
        ReviewStatus Status
        datetime CreatedAt
    }
    BuyerReview {
        uuid Id PK
        uuid OrderId FK
        uuid SellerId FK
        uuid BuyerId FK
        int Rating
        string Comment
        ReviewStatus Status
        datetime CreatedAt
    }
    SellerRatingSummary {
        uuid Id PK
        uuid SellerId FK
        decimal AverageRating
        int TotalReviews
    }
    ReviewMedia {
        uuid Id PK
        uuid ReviewId FK
        string Url
    }
    ReviewReport {
        uuid Id PK
        uuid ReviewId FK
        uuid ReporterId FK
        ReviewReportReason Reason
        ReviewReportStatus Status
    }
    ReviewVote {
        uuid Id PK
        uuid ReviewId FK
        uuid UserId FK
        bool IsHelpful
    }

    Order ||--o| SellerReview : "buyer reviews seller"
    Order ||--o| BuyerReview : "seller reviews buyer"
    User ||--o| SellerRatingSummary : summarizes
    SellerReview ||--o{ ReviewMedia : has
    SellerReview ||--o{ ReviewReport : reported
    SellerReview ||--o{ ReviewVote : voted
```
