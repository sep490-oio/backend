// using System;
// using System.Collections.Generic;
// using Microsoft.EntityFrameworkCore;
// using OIO.Infrastructure.Databases.DataContext;
//
// namespace OIO.Infrastructure.Databases;
//
// public partial class ApplicationDbContext : DbContext
// {
//     public ApplicationDbContext()
//     {
//     }
//
//     public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
//         : base(options)
//     {
//     }
//
//     public virtual DbSet<Auction> Auctions { get; set; }
//
//     public virtual DbSet<AuctionAutoBid> AuctionAutoBids { get; set; }
//
//     public virtual DbSet<AuctionDeposit> AuctionDeposits { get; set; }
//
//     public virtual DbSet<AuctionPriceHistory> AuctionPriceHistories { get; set; }
//
//     public virtual DbSet<AuctionWatcher> AuctionWatchers { get; set; }
//
//     public virtual DbSet<Bid> Bids { get; set; }
//
//     public virtual DbSet<BuyerReview> BuyerReviews { get; set; }
//
//     public virtual DbSet<Category> Categories { get; set; }
//
//     public virtual DbSet<Dispute> Disputes { get; set; }
//
//     public virtual DbSet<DisputeEvidence> DisputeEvidences { get; set; }
//
//     public virtual DbSet<DisputeMessage> DisputeMessages { get; set; }
//
//     public virtual DbSet<DisputeRefund> DisputeRefunds { get; set; }
//
//     public virtual DbSet<DisputeResponseTemplate> DisputeResponseTemplates { get; set; }
//
//     public virtual DbSet<DisputeStatusHistory> DisputeStatusHistories { get; set; }
//
//     public virtual DbSet<Escrow> Escrows { get; set; }
//
//     public virtual DbSet<Invoice> Invoices { get; set; }
//
//     public virtual DbSet<Item> Items { get; set; }
//
//     public virtual DbSet<ItemImage> ItemImages { get; set; }
//
//     public virtual DbSet<ItemQuestion> ItemQuestions { get; set; }
//
//     public virtual DbSet<Notification> Notifications { get; set; }
//
//     public virtual DbSet<NotificationDelivery> NotificationDeliveries { get; set; }
//
//     public virtual DbSet<Order> Orders { get; set; }
//
//     public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }
//
//     public virtual DbSet<Permission> Permissions { get; set; }
//
//     public virtual DbSet<ReviewImage> ReviewImages { get; set; }
//
//     public virtual DbSet<ReviewReport> ReviewReports { get; set; }
//
//     public virtual DbSet<ReviewVote> ReviewVotes { get; set; }
//
//     public virtual DbSet<Role> Roles { get; set; }
//
//     public virtual DbSet<RolePermission> RolePermissions { get; set; }
//
//     public virtual DbSet<SellerKyc> SellerKycs { get; set; }
//
//     public virtual DbSet<SellerKycDocument> SellerKycDocuments { get; set; }
//
//     public virtual DbSet<SellerKycHistory> SellerKycHistories { get; set; }
//
//     public virtual DbSet<SellerProfile> SellerProfiles { get; set; }
//
//     public virtual DbSet<SellerRatingSummary> SellerRatingSummaries { get; set; }
//
//     public virtual DbSet<SellerReview> SellerReviews { get; set; }
//
//     public virtual DbSet<Shipment> Shipments { get; set; }
//
//     public virtual DbSet<Transaction> Transactions { get; set; }
//
//     public virtual DbSet<User> Users { get; set; }
//
//     public virtual DbSet<UserAddress> UserAddresses { get; set; }
//
//     public virtual DbSet<UserLoginHistory> UserLoginHistories { get; set; }
//
//     public virtual DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
//
//     public virtual DbSet<UserPermission> UserPermissions { get; set; }
//
//     public virtual DbSet<UserProfile> UserProfiles { get; set; }
//
//     public virtual DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
//
//     public virtual DbSet<UserRefreshTokenFamily> UserRefreshTokenFamilies { get; set; }
//
//     public virtual DbSet<UserRole> UserRoles { get; set; }
//
//     public virtual DbSet<Wallet> Wallets { get; set; }
//
//     public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }
//
//     public virtual DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }
//
//     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
// #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//         => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=oio_database;Username=postgres;Password=Strongbow22;Include Error Detail=true;");
//
//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         modelBuilder.Entity<Auction>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("auctions_pkey");
//
//             entity.ToTable("auctions");
//
//             entity.HasIndex(e => new { e.Status, e.EndTime }, "idx_auctions_active").HasFilter("((status)::text = 'active'::text)");
//
//             entity.HasIndex(e => e.EndTime, "idx_auctions_end_time");
//
//             entity.HasIndex(e => e.StartTime, "idx_auctions_start_time");
//
//             entity.HasIndex(e => e.Status, "idx_auctions_status");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.ActualEndTime).HasColumnName("actual_end_time");
//             entity.Property(e => e.AutoExtend)
//                 .HasDefaultValue(true)
//                 .HasColumnName("auto_extend");
//             entity.Property(e => e.BidCount)
//                 .HasDefaultValue(0)
//                 .HasColumnName("bid_count");
//             entity.Property(e => e.BidIncrement)
//                 .HasPrecision(18, 2)
//                 .HasDefaultValue(1.00m)
//                 .HasColumnName("bid_increment");
//             entity.Property(e => e.BuyNowPrice)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("buy_now_price");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Currency)
//                 .HasMaxLength(3)
//                 .HasDefaultValueSql("'VND'::character varying")
//                 .HasColumnName("currency");
//             entity.Property(e => e.CurrentPrice)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("current_price");
//             entity.Property(e => e.EndTime).HasColumnName("end_time");
//             entity.Property(e => e.ExtensionMinutes)
//                 .HasDefaultValue(5)
//                 .HasColumnName("extension_minutes");
//             entity.Property(e => e.IsFeatured)
//                 .HasDefaultValue(false)
//                 .HasColumnName("is_featured");
//             entity.Property(e => e.ItemId).HasColumnName("item_id");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.ReservePrice)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("reserve_price");
//             entity.Property(e => e.StartTime).HasColumnName("start_time");
//             entity.Property(e => e.StartingPrice)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("starting_price");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'draft'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.ViewCount)
//                 .HasDefaultValue(0)
//                 .HasColumnName("view_count");
//             entity.Property(e => e.WatchCount)
//                 .HasDefaultValue(0)
//                 .HasColumnName("watch_count");
//             entity.Property(e => e.WinnerId).HasColumnName("winner_id");
//
//             entity.HasOne(d => d.Item).WithMany(p => p.Auctions)
//                 .HasForeignKey(d => d.ItemId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("auctions_item_id_fkey");
//
//             entity.HasOne(d => d.Winner).WithMany(p => p.Auctions)
//                 .HasForeignKey(d => d.WinnerId)
//                 .HasConstraintName("auctions_winner_id_fkey");
//         });
//
//         modelBuilder.Entity<AuctionAutoBid>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("auction_auto_bids_pkey");
//
//             entity.ToTable("auction_auto_bids");
//
//             entity.HasIndex(e => new { e.AuctionId, e.BidderId }, "auction_auto_bids_auction_id_bidder_id_key").IsUnique();
//
//             entity.HasIndex(e => e.AuctionId, "idx_auction_auto_bids_auction_id");
//
//             entity.HasIndex(e => new { e.AuctionId, e.Status }, "idx_auction_auto_bids_auction_id_status").HasFilter("((status)::text = 'active'::text)");
//
//             entity.HasIndex(e => e.BidderId, "idx_auction_auto_bids_bidder_id");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AuctionId).HasColumnName("auction_id");
//             entity.Property(e => e.BidderId).HasColumnName("bidder_id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.CurrentAmount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("current_amount");
//             entity.Property(e => e.IncrementAmount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("increment_amount");
//             entity.Property(e => e.IsEnabled)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_enabled");
//             entity.Property(e => e.LastAutoBidAt).HasColumnName("last_auto_bid_at");
//             entity.Property(e => e.MaxAmount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("max_amount");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'active'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.TotalAutoBids).HasColumnName("total_auto_bids");
//
//             entity.HasOne(d => d.Auction).WithMany(p => p.AuctionAutoBids)
//                 .HasForeignKey(d => d.AuctionId)
//                 .HasConstraintName("auction_auto_bids_auction_id_fkey");
//
//             entity.HasOne(d => d.Bidder).WithMany(p => p.AuctionAutoBids)
//                 .HasForeignKey(d => d.BidderId)
//                 .HasConstraintName("auction_auto_bids_bidder_id_fkey");
//         });
//
//         modelBuilder.Entity<AuctionDeposit>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("auction_deposits_pkey");
//
//             entity.ToTable("auction_deposits");
//
//             entity.HasIndex(e => new { e.AuctionId, e.UserId }, "auction_deposits_auction_id_user_id_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Amount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("amount");
//             entity.Property(e => e.AuctionId).HasColumnName("auction_id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.ReleasedAt).HasColumnName("released_at");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'held'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.Auction).WithMany(p => p.AuctionDeposits)
//                 .HasForeignKey(d => d.AuctionId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("auction_deposits_auction_id_fkey");
//
//             entity.HasOne(d => d.Transaction).WithMany(p => p.AuctionDeposits)
//                 .HasForeignKey(d => d.TransactionId)
//                 .HasConstraintName("auction_deposits_transaction_id_fkey");
//
//             entity.HasOne(d => d.User).WithMany(p => p.AuctionDeposits)
//                 .HasForeignKey(d => d.UserId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("auction_deposits_user_id_fkey");
//         });
//
//         modelBuilder.Entity<AuctionPriceHistory>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("auction_price_history_pkey");
//
//             entity.ToTable("auction_price_history");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AuctionId).HasColumnName("auction_id");
//             entity.Property(e => e.BidId).HasColumnName("bid_id");
//             entity.Property(e => e.Price)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("price");
//             entity.Property(e => e.RecordedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("recorded_at");
//
//             entity.HasOne(d => d.Auction).WithMany(p => p.AuctionPriceHistories)
//                 .HasForeignKey(d => d.AuctionId)
//                 .HasConstraintName("auction_price_history_auction_id_fkey");
//
//             entity.HasOne(d => d.Bid).WithMany(p => p.AuctionPriceHistories)
//                 .HasForeignKey(d => d.BidId)
//                 .HasConstraintName("auction_price_history_bid_id_fkey");
//         });
//
//         modelBuilder.Entity<AuctionWatcher>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("auction_watchers_pkey");
//
//             entity.ToTable("auction_watchers");
//
//             entity.HasIndex(e => new { e.AuctionId, e.UserId }, "auction_watchers_auction_id_user_id_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AuctionId).HasColumnName("auction_id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.NotifyOnBid)
//                 .HasDefaultValue(true)
//                 .HasColumnName("notify_on_bid");
//             entity.Property(e => e.NotifyOnEnd)
//                 .HasDefaultValue(true)
//                 .HasColumnName("notify_on_end");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.Auction).WithMany(p => p.AuctionWatchers)
//                 .HasForeignKey(d => d.AuctionId)
//                 .HasConstraintName("auction_watchers_auction_id_fkey");
//
//             entity.HasOne(d => d.User).WithMany(p => p.AuctionWatchers)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("auction_watchers_user_id_fkey");
//         });
//
//         modelBuilder.Entity<Bid>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("bids_pkey");
//
//             entity.ToTable("bids");
//
//             entity.HasIndex(e => new { e.AuctionId, e.Amount }, "idx_bids_amount").IsDescending(false, true);
//
//             entity.HasIndex(e => e.AuctionId, "idx_bids_auction");
//
//             entity.HasIndex(e => e.BidderId, "idx_bids_bidder");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Amount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("amount");
//             entity.Property(e => e.AuctionId).HasColumnName("auction_id");
//             entity.Property(e => e.AutoBidId).HasColumnName("auto_bid_id");
//             entity.Property(e => e.BidderId).HasColumnName("bidder_id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.IpAddress).HasColumnName("ip_address");
//             entity.Property(e => e.IsAutoBid)
//                 .HasComputedColumnSql("(auto_bid_id IS NOT NULL)", true)
//                 .HasColumnName("is_auto_bid");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'active'::character varying")
//                 .HasColumnName("status");
//
//             entity.HasOne(d => d.Auction).WithMany(p => p.Bids)
//                 .HasForeignKey(d => d.AuctionId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("bids_auction_id_fkey");
//
//             entity.HasOne(d => d.AutoBid).WithMany(p => p.Bids)
//                 .HasForeignKey(d => d.AutoBidId)
//                 .HasConstraintName("bids_auto_bid_id_fkey");
//
//             entity.HasOne(d => d.Bidder).WithMany(p => p.Bids)
//                 .HasForeignKey(d => d.BidderId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("bids_bidder_id_fkey");
//         });
//
//         modelBuilder.Entity<BuyerReview>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("buyer_reviews_pkey");
//
//             entity.ToTable("buyer_reviews");
//
//             entity.HasIndex(e => new { e.OrderId, e.ReviewerId }, "buyer_reviews_order_id_reviewer_id_key").IsUnique();
//
//             entity.HasIndex(e => e.BuyerId, "idx_buyer_reviews_buyer");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.BuyerId).HasColumnName("buyer_id");
//             entity.Property(e => e.Comment).HasColumnName("comment");
//             entity.Property(e => e.CommunicationRating).HasColumnName("communication_rating");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.OrderId).HasColumnName("order_id");
//             entity.Property(e => e.OverallRating).HasColumnName("overall_rating");
//             entity.Property(e => e.PaymentSpeedRating).HasColumnName("payment_speed_rating");
//             entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'published'::character varying")
//                 .HasColumnName("status");
//
//             entity.HasOne(d => d.Buyer).WithMany(p => p.BuyerReviewBuyers)
//                 .HasForeignKey(d => d.BuyerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("buyer_reviews_buyer_id_fkey");
//
//             entity.HasOne(d => d.Order).WithMany(p => p.BuyerReviews)
//                 .HasForeignKey(d => d.OrderId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("buyer_reviews_order_id_fkey");
//
//             entity.HasOne(d => d.Reviewer).WithMany(p => p.BuyerReviewReviewers)
//                 .HasForeignKey(d => d.ReviewerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("buyer_reviews_reviewer_id_fkey");
//         });
//
//         modelBuilder.Entity<Category>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("categories_pkey");
//
//             entity.ToTable("categories");
//
//             entity.HasIndex(e => e.Slug, "categories_slug_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Description).HasColumnName("description");
//             entity.Property(e => e.IconUrl)
//                 .HasMaxLength(500)
//                 .HasColumnName("icon_url");
//             entity.Property(e => e.IsActive)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_active");
//             entity.Property(e => e.Name)
//                 .HasMaxLength(100)
//                 .HasColumnName("name");
//             entity.Property(e => e.ParentId).HasColumnName("parent_id");
//             entity.Property(e => e.Path).HasColumnName("path");
//             entity.Property(e => e.Slug)
//                 .HasMaxLength(100)
//                 .HasColumnName("slug");
//             entity.Property(e => e.SortOrder)
//                 .HasDefaultValue(0)
//                 .HasColumnName("sort_order");
//
//             entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
//                 .HasForeignKey(d => d.ParentId)
//                 .HasConstraintName("categories_parent_id_fkey");
//         });
//
//         modelBuilder.Entity<Dispute>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("disputes_pkey");
//
//             entity.ToTable("disputes");
//
//             entity.HasIndex(e => e.DisputeNumber, "disputes_dispute_number_key").IsUnique();
//
//             entity.HasIndex(e => e.AssignedTo, "idx_disputes_assigned");
//
//             entity.HasIndex(e => e.ComplainantId, "idx_disputes_complainant");
//
//             entity.HasIndex(e => e.OrderId, "idx_disputes_order");
//
//             entity.HasIndex(e => e.RespondentId, "idx_disputes_respondent");
//
//             entity.HasIndex(e => e.Status, "idx_disputes_status");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AssignedTo).HasColumnName("assigned_to");
//             entity.Property(e => e.AuctionId).HasColumnName("auction_id");
//             entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
//             entity.Property(e => e.ComplainantId).HasColumnName("complainant_id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Description).HasColumnName("description");
//             entity.Property(e => e.DesiredResolution)
//                 .HasMaxLength(50)
//                 .HasColumnName("desired_resolution");
//             entity.Property(e => e.DisputeNumber)
//                 .HasMaxLength(50)
//                 .HasColumnName("dispute_number");
//             entity.Property(e => e.EscalatedAt).HasColumnName("escalated_at");
//             entity.Property(e => e.EscalatedTo).HasColumnName("escalated_to");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.OrderId).HasColumnName("order_id");
//             entity.Property(e => e.Priority)
//                 .HasMaxLength(10)
//                 .HasDefaultValueSql("'medium'::character varying")
//                 .HasColumnName("priority");
//             entity.Property(e => e.ResolutionAmount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("resolution_amount");
//             entity.Property(e => e.ResolutionNotes).HasColumnName("resolution_notes");
//             entity.Property(e => e.ResolutionType)
//                 .HasMaxLength(30)
//                 .HasColumnName("resolution_type");
//             entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
//             entity.Property(e => e.RespondentId).HasColumnName("respondent_id");
//             entity.Property(e => e.ResponseDeadline).HasColumnName("response_deadline");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(30)
//                 .HasDefaultValueSql("'open'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.Title)
//                 .HasMaxLength(255)
//                 .HasColumnName("title");
//             entity.Property(e => e.Type)
//                 .HasMaxLength(50)
//                 .HasColumnName("type");
//
//             entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.DisputeAssignedToNavigations)
//                 .HasForeignKey(d => d.AssignedTo)
//                 .HasConstraintName("disputes_assigned_to_fkey");
//
//             entity.HasOne(d => d.Auction).WithMany(p => p.Disputes)
//                 .HasForeignKey(d => d.AuctionId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("disputes_auction_id_fkey");
//
//             entity.HasOne(d => d.Complainant).WithMany(p => p.DisputeComplainants)
//                 .HasForeignKey(d => d.ComplainantId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("disputes_complainant_id_fkey");
//
//             entity.HasOne(d => d.EscalatedToNavigation).WithMany(p => p.DisputeEscalatedToNavigations)
//                 .HasForeignKey(d => d.EscalatedTo)
//                 .HasConstraintName("disputes_escalated_to_fkey");
//
//             entity.HasOne(d => d.Order).WithMany(p => p.Disputes)
//                 .HasForeignKey(d => d.OrderId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("disputes_order_id_fkey");
//
//             entity.HasOne(d => d.Respondent).WithMany(p => p.DisputeRespondents)
//                 .HasForeignKey(d => d.RespondentId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("disputes_respondent_id_fkey");
//         });
//
//         modelBuilder.Entity<DisputeEvidence>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("dispute_evidence_pkey");
//
//             entity.ToTable("dispute_evidence");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Description).HasColumnName("description");
//             entity.Property(e => e.DisputeId).HasColumnName("dispute_id");
//             entity.Property(e => e.FileName)
//                 .HasMaxLength(255)
//                 .HasColumnName("file_name");
//             entity.Property(e => e.FileUrl)
//                 .HasMaxLength(500)
//                 .HasColumnName("file_url");
//             entity.Property(e => e.SubmittedBy).HasColumnName("submitted_by");
//             entity.Property(e => e.Type)
//                 .HasMaxLength(30)
//                 .HasColumnName("type");
//
//             entity.HasOne(d => d.Dispute).WithMany(p => p.DisputeEvidences)
//                 .HasForeignKey(d => d.DisputeId)
//                 .HasConstraintName("dispute_evidence_dispute_id_fkey");
//
//             entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.DisputeEvidences)
//                 .HasForeignKey(d => d.SubmittedBy)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("dispute_evidence_submitted_by_fkey");
//         });
//
//         modelBuilder.Entity<DisputeMessage>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("dispute_messages_pkey");
//
//             entity.ToTable("dispute_messages");
//
//             entity.HasIndex(e => e.DisputeId, "idx_dispute_messages_dispute");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.DisputeId).HasColumnName("dispute_id");
//             entity.Property(e => e.IsInternal)
//                 .HasDefaultValue(false)
//                 .HasColumnName("is_internal");
//             entity.Property(e => e.Message).HasColumnName("message");
//             entity.Property(e => e.SenderId).HasColumnName("sender_id");
//
//             entity.HasOne(d => d.Dispute).WithMany(p => p.DisputeMessages)
//                 .HasForeignKey(d => d.DisputeId)
//                 .HasConstraintName("dispute_messages_dispute_id_fkey");
//
//             entity.HasOne(d => d.Sender).WithMany(p => p.DisputeMessages)
//                 .HasForeignKey(d => d.SenderId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("dispute_messages_sender_id_fkey");
//         });
//
//         modelBuilder.Entity<DisputeRefund>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("dispute_refunds_pkey");
//
//             entity.ToTable("dispute_refunds");
//
//             entity.HasIndex(e => e.TransactionId, "dispute_refunds_transaction_id_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
//             entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.DisputeId).HasColumnName("dispute_id");
//             entity.Property(e => e.Notes).HasColumnName("notes");
//             entity.Property(e => e.Reason).HasColumnName("reason");
//             entity.Property(e => e.RefundType)
//                 .HasMaxLength(30)
//                 .HasColumnName("refund_type");
//             entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
//
//             entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.DisputeRefunds)
//                 .HasForeignKey(d => d.ApprovedBy)
//                 .HasConstraintName("dispute_refunds_approved_by_fkey");
//
//             entity.HasOne(d => d.Dispute).WithMany(p => p.DisputeRefunds)
//                 .HasForeignKey(d => d.DisputeId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("dispute_refunds_dispute_id_fkey");
//
//             entity.HasOne(d => d.Transaction).WithOne(p => p.DisputeRefund)
//                 .HasForeignKey<DisputeRefund>(d => d.TransactionId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("dispute_refunds_transaction_id_fkey");
//         });
//
//         modelBuilder.Entity<DisputeResponseTemplate>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("dispute_response_templates_pkey");
//
//             entity.ToTable("dispute_response_templates");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Body).HasColumnName("body");
//             entity.Property(e => e.Category)
//                 .HasMaxLength(50)
//                 .HasColumnName("category");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.IsActive)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_active");
//             entity.Property(e => e.Name)
//                 .HasMaxLength(100)
//                 .HasColumnName("name");
//             entity.Property(e => e.Subject)
//                 .HasMaxLength(255)
//                 .HasColumnName("subject");
//         });
//
//         modelBuilder.Entity<DisputeStatusHistory>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("dispute_status_history_pkey");
//
//             entity.ToTable("dispute_status_history");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.DisputeId).HasColumnName("dispute_id");
//             entity.Property(e => e.NewStatus)
//                 .HasMaxLength(30)
//                 .HasColumnName("new_status");
//             entity.Property(e => e.OldStatus)
//                 .HasMaxLength(30)
//                 .HasColumnName("old_status");
//             entity.Property(e => e.Reason).HasColumnName("reason");
//
//             entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.DisputeStatusHistories)
//                 .HasForeignKey(d => d.ChangedBy)
//                 .HasConstraintName("dispute_status_history_changed_by_fkey");
//
//             entity.HasOne(d => d.Dispute).WithMany(p => p.DisputeStatusHistories)
//                 .HasForeignKey(d => d.DisputeId)
//                 .HasConstraintName("dispute_status_history_dispute_id_fkey");
//         });
//
//         modelBuilder.Entity<Escrow>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("escrows_pkey");
//
//             entity.ToTable("escrows");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Amount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("amount");
//             entity.Property(e => e.Currency)
//                 .HasMaxLength(3)
//                 .HasDefaultValueSql("'VND'::character varying")
//                 .HasColumnName("currency");
//             entity.Property(e => e.HeldAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("held_at");
//             entity.Property(e => e.HoldTransactionId).HasColumnName("hold_transaction_id");
//             entity.Property(e => e.OrderId).HasColumnName("order_id");
//             entity.Property(e => e.ReleaseTransactionId).HasColumnName("release_transaction_id");
//             entity.Property(e => e.ReleasedAt).HasColumnName("released_at");
//             entity.Property(e => e.ReleasedTo)
//                 .HasMaxLength(20)
//                 .HasColumnName("released_to");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'holding'::character varying")
//                 .HasColumnName("status");
//
//             entity.HasOne(d => d.HoldTransaction).WithMany(p => p.EscrowHoldTransactions)
//                 .HasForeignKey(d => d.HoldTransactionId)
//                 .HasConstraintName("escrows_hold_transaction_id_fkey");
//
//             entity.HasOne(d => d.Order).WithMany(p => p.Escrows)
//                 .HasForeignKey(d => d.OrderId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("escrows_order_id_fkey");
//
//             entity.HasOne(d => d.ReleaseTransaction).WithMany(p => p.EscrowReleaseTransactions)
//                 .HasForeignKey(d => d.ReleaseTransactionId)
//                 .HasConstraintName("escrows_release_transaction_id_fkey");
//         });
//
//         modelBuilder.Entity<Invoice>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("invoices_pkey");
//
//             entity.ToTable("invoices");
//
//             entity.HasIndex(e => e.InvoiceNumber, "invoices_invoice_number_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.BuyerId).HasColumnName("buyer_id");
//             entity.Property(e => e.Currency)
//                 .HasMaxLength(3)
//                 .HasDefaultValueSql("'VND'::character varying")
//                 .HasColumnName("currency");
//             entity.Property(e => e.DueDate).HasColumnName("due_date");
//             entity.Property(e => e.InvoiceNumber)
//                 .HasMaxLength(50)
//                 .HasColumnName("invoice_number");
//             entity.Property(e => e.IssuedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("issued_at");
//             entity.Property(e => e.OrderId).HasColumnName("order_id");
//             entity.Property(e => e.PaidAt).HasColumnName("paid_at");
//             entity.Property(e => e.SellerId).HasColumnName("seller_id");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'issued'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.Subtotal)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("subtotal");
//             entity.Property(e => e.TaxAmount)
//                 .HasPrecision(18, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("tax_amount");
//             entity.Property(e => e.TotalAmount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("total_amount");
//
//             entity.HasOne(d => d.Buyer).WithMany(p => p.InvoiceBuyers)
//                 .HasForeignKey(d => d.BuyerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("invoices_buyer_id_fkey");
//
//             entity.HasOne(d => d.Order).WithMany(p => p.Invoices)
//                 .HasForeignKey(d => d.OrderId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("invoices_order_id_fkey");
//
//             entity.HasOne(d => d.Seller).WithMany(p => p.InvoiceSellers)
//                 .HasForeignKey(d => d.SellerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("invoices_seller_id_fkey");
//         });
//
//         modelBuilder.Entity<Item>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("items_pkey");
//
//             entity.ToTable("items");
//
//             entity.HasIndex(e => e.CategoryId, "idx_items_category");
//
//             entity.HasIndex(e => e.SellerId, "idx_items_seller");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Attributes)
//                 .HasColumnType("jsonb")
//                 .HasColumnName("attributes");
//             entity.Property(e => e.CategoryId).HasColumnName("category_id");
//             entity.Property(e => e.Condition)
//                 .HasMaxLength(50)
//                 .HasColumnName("condition");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Description).HasColumnName("description");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.Quantity)
//                 .HasDefaultValue(1)
//                 .HasColumnName("quantity");
//             entity.Property(e => e.SellerId).HasColumnName("seller_id");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'draft'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.Title)
//                 .HasMaxLength(255)
//                 .HasColumnName("title");
//
//             entity.HasOne(d => d.Category).WithMany(p => p.Items)
//                 .HasForeignKey(d => d.CategoryId)
//                 .HasConstraintName("items_category_id_fkey");
//
//             entity.HasOne(d => d.Seller).WithMany(p => p.Items)
//                 .HasForeignKey(d => d.SellerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("items_seller_id_fkey");
//         });
//
//         modelBuilder.Entity<ItemImage>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("item_images_pkey");
//
//             entity.ToTable("item_images");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.ImageUrl)
//                 .HasMaxLength(500)
//                 .HasColumnName("image_url");
//             entity.Property(e => e.IsPrimary)
//                 .HasDefaultValue(false)
//                 .HasColumnName("is_primary");
//             entity.Property(e => e.ItemId).HasColumnName("item_id");
//             entity.Property(e => e.SortOrder)
//                 .HasDefaultValue(0)
//                 .HasColumnName("sort_order");
//
//             entity.HasOne(d => d.Item).WithMany(p => p.ItemImages)
//                 .HasForeignKey(d => d.ItemId)
//                 .HasConstraintName("item_images_item_id_fkey");
//         });
//
//         modelBuilder.Entity<ItemQuestion>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("item_questions_pkey");
//
//             entity.ToTable("item_questions");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Answer).HasColumnName("answer");
//             entity.Property(e => e.AnsweredAt).HasColumnName("answered_at");
//             entity.Property(e => e.AskerId).HasColumnName("asker_id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.IsPublic)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_public");
//             entity.Property(e => e.ItemId).HasColumnName("item_id");
//             entity.Property(e => e.Question).HasColumnName("question");
//
//             entity.HasOne(d => d.Asker).WithMany(p => p.ItemQuestions)
//                 .HasForeignKey(d => d.AskerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("item_questions_asker_id_fkey");
//
//             entity.HasOne(d => d.Item).WithMany(p => p.ItemQuestions)
//                 .HasForeignKey(d => d.ItemId)
//                 .HasConstraintName("item_questions_item_id_fkey");
//         });
//
//         modelBuilder.Entity<Notification>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("notifications_pkey");
//
//             entity.ToTable("notifications");
//
//             entity.HasIndex(e => new { e.EntityType, e.EntityId }, "idx_notifications_entity").HasFilter("(entity_id IS NOT NULL)");
//
//             entity.HasIndex(e => e.Metadata, "idx_notifications_metadata").HasMethod("gin");
//
//             entity.HasIndex(e => new { e.Priority, e.CreatedAt }, "idx_notifications_priority_created")
//                 .IsDescending(false, true)
//                 .HasFilter("((status)::text = 'unread'::text)");
//
//             entity.HasIndex(e => new { e.NotificationType, e.EventType }, "idx_notifications_type_event");
//
//             entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_notifications_user_created").IsDescending(false, true);
//
//             entity.HasIndex(e => new { e.UserId, e.Status }, "idx_notifications_user_status");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Actions)
//                 .HasDefaultValueSql("'[]'::jsonb")
//                 .HasColumnType("jsonb")
//                 .HasColumnName("actions");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.EntityId).HasColumnName("entity_id");
//             entity.Property(e => e.EntityType)
//                 .HasMaxLength(50)
//                 .HasColumnName("entity_type");
//             entity.Property(e => e.EventType)
//                 .HasMaxLength(50)
//                 .HasColumnName("event_type");
//             entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
//             entity.Property(e => e.Message).HasColumnName("message");
//             entity.Property(e => e.Metadata)
//                 .HasDefaultValueSql("'{}'::jsonb")
//                 .HasColumnType("jsonb")
//                 .HasColumnName("metadata");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.NotificationType)
//                 .HasMaxLength(50)
//                 .HasColumnName("notification_type");
//             entity.Property(e => e.Priority)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'normal'::character varying")
//                 .HasColumnName("priority");
//             entity.Property(e => e.ReadAt).HasColumnName("read_at");
//             entity.Property(e => e.RelatedEntities)
//                 .HasDefaultValueSql("'[]'::jsonb")
//                 .HasColumnType("jsonb")
//                 .HasColumnName("related_entities");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'unread'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.Title)
//                 .HasMaxLength(500)
//                 .HasColumnName("title");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.User).WithMany(p => p.Notifications)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("notifications_user_id_fkey");
//         });
//
//         modelBuilder.Entity<NotificationDelivery>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("notification_delivery_pkey");
//
//             entity.ToTable("notification_delivery");
//
//             entity.HasIndex(e => e.NotificationId, "idx_delivery_notification");
//
//             entity.HasIndex(e => e.NextRetryAt, "idx_delivery_retry").HasFilter("(((status)::text = 'failed'::text) AND (next_retry_at IS NOT NULL))");
//
//             entity.HasIndex(e => new { e.Status, e.ScheduledAt }, "idx_delivery_status_scheduled").HasFilter("((status)::text = 'pending'::text)");
//
//             entity.HasIndex(e => new { e.UserId, e.Channel }, "idx_delivery_user_channel");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AttemptCount)
//                 .HasDefaultValue(0)
//                 .HasColumnName("attempt_count");
//             entity.Property(e => e.Channel)
//                 .HasMaxLength(20)
//                 .HasColumnName("channel");
//             entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
//             entity.Property(e => e.DeliveryMetadata)
//                 .HasDefaultValueSql("'{}'::jsonb")
//                 .HasColumnType("jsonb")
//                 .HasColumnName("delivery_metadata");
//             entity.Property(e => e.ErrorCode)
//                 .HasMaxLength(50)
//                 .HasColumnName("error_code");
//             entity.Property(e => e.ErrorDetails)
//                 .HasColumnType("jsonb")
//                 .HasColumnName("error_details");
//             entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
//             entity.Property(e => e.FailedAt).HasColumnName("failed_at");
//             entity.Property(e => e.MaxAttempts)
//                 .HasDefaultValue(3)
//                 .HasColumnName("max_attempts");
//             entity.Property(e => e.NextRetryAt)
//                 .HasColumnType("timestamp without time zone")
//                 .HasColumnName("next_retry_at");
//             entity.Property(e => e.NotificationId).HasColumnName("notification_id");
//             entity.Property(e => e.ScheduledAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("scheduled_at");
//             entity.Property(e => e.SentAt).HasColumnName("sent_at");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'pending'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.Notification).WithMany(p => p.NotificationDeliveries)
//                 .HasForeignKey(d => d.NotificationId)
//                 .HasConstraintName("notification_delivery_notification_id_fkey");
//
//             entity.HasOne(d => d.User).WithMany(p => p.NotificationDeliveries)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("notification_delivery_user_id_fkey");
//         });
//
//         modelBuilder.Entity<Order>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("orders_pkey");
//
//             entity.ToTable("orders");
//
//             entity.HasIndex(e => e.BuyerId, "idx_orders_buyer");
//
//             entity.HasIndex(e => e.SellerId, "idx_orders_seller");
//
//             entity.HasIndex(e => e.Status, "idx_orders_status");
//
//             entity.HasIndex(e => e.OrderNumber, "orders_order_number_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AuctionId).HasColumnName("auction_id");
//             entity.Property(e => e.BillingAddressId).HasColumnName("billing_address_id");
//             entity.Property(e => e.BuyerId).HasColumnName("buyer_id");
//             entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
//             entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Currency)
//                 .HasMaxLength(3)
//                 .HasDefaultValueSql("'VND'::character varying")
//                 .HasColumnName("currency");
//             entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
//             entity.Property(e => e.ItemPrice)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("item_price");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.Notes).HasColumnName("notes");
//             entity.Property(e => e.OrderNumber)
//                 .HasMaxLength(50)
//                 .HasColumnName("order_number");
//             entity.Property(e => e.PaidAt).HasColumnName("paid_at");
//             entity.Property(e => e.PlatformFee)
//                 .HasPrecision(18, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("platform_fee");
//             entity.Property(e => e.SellerId).HasColumnName("seller_id");
//             entity.Property(e => e.ShippedAt).HasColumnName("shipped_at");
//             entity.Property(e => e.ShippingAddress).HasColumnName("shipping_address");
//             entity.Property(e => e.ShippingAddressId).HasColumnName("shipping_address_id");
//             entity.Property(e => e.ShippingCity)
//                 .HasMaxLength(120)
//                 .HasColumnName("shipping_city");
//             entity.Property(e => e.ShippingDistrict)
//                 .HasMaxLength(100)
//                 .HasColumnName("shipping_district");
//             entity.Property(e => e.ShippingFee)
//                 .HasPrecision(18, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("shipping_fee");
//             entity.Property(e => e.ShippingPhone)
//                 .HasMaxLength(20)
//                 .HasColumnName("shipping_phone");
//             entity.Property(e => e.ShippingRecipientName)
//                 .HasMaxLength(100)
//                 .HasColumnName("shipping_recipient_name");
//             entity.Property(e => e.ShippingWard)
//                 .HasMaxLength(100)
//                 .HasColumnName("shipping_ward");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(30)
//                 .HasDefaultValueSql("'pending_payment'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.TaxAmount)
//                 .HasPrecision(18, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("tax_amount");
//             entity.Property(e => e.TotalAmount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("total_amount");
//             entity.Property(e => e.Version).HasColumnName("version");
//
//             entity.HasOne(d => d.Auction).WithMany(p => p.Orders)
//                 .HasForeignKey(d => d.AuctionId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("orders_auction_id_fkey");
//
//             entity.HasOne(d => d.BillingAddress).WithMany(p => p.OrderBillingAddresses)
//                 .HasForeignKey(d => d.BillingAddressId)
//                 .HasConstraintName("orders_billing_address_id_fkey");
//
//             entity.HasOne(d => d.Buyer).WithMany(p => p.OrderBuyers)
//                 .HasForeignKey(d => d.BuyerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("orders_buyer_id_fkey");
//
//             entity.HasOne(d => d.Seller).WithMany(p => p.OrderSellers)
//                 .HasForeignKey(d => d.SellerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("orders_seller_id_fkey");
//
//             entity.HasOne(d => d.ShippingAddressNavigation).WithMany(p => p.OrderShippingAddressNavigations)
//                 .HasForeignKey(d => d.ShippingAddressId)
//                 .HasConstraintName("orders_shipping_address_id_fkey");
//         });
//
//         modelBuilder.Entity<PaymentMethod>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("payment_methods_pkey");
//
//             entity.ToTable("payment_methods");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.ExpiryMonth).HasColumnName("expiry_month");
//             entity.Property(e => e.ExpiryYear).HasColumnName("expiry_year");
//             entity.Property(e => e.HolderName)
//                 .HasMaxLength(200)
//                 .HasColumnName("holder_name");
//             entity.Property(e => e.IsActive)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_active");
//             entity.Property(e => e.IsDefault)
//                 .HasDefaultValue(false)
//                 .HasColumnName("is_default");
//             entity.Property(e => e.IsVerified)
//                 .HasDefaultValue(false)
//                 .HasColumnName("is_verified");
//             entity.Property(e => e.LastFour)
//                 .HasMaxLength(4)
//                 .HasColumnName("last_four");
//             entity.Property(e => e.Provider)
//                 .HasMaxLength(50)
//                 .HasColumnName("provider");
//             entity.Property(e => e.TokenReference)
//                 .HasMaxLength(255)
//                 .HasColumnName("token_reference");
//             entity.Property(e => e.Type)
//                 .HasMaxLength(30)
//                 .HasColumnName("type");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.User).WithMany(p => p.PaymentMethods)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("payment_methods_user_id_fkey");
//         });
//
//         modelBuilder.Entity<Permission>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("permissions_pkey");
//
//             entity.ToTable("permissions");
//
//             entity.HasIndex(e => e.NormalizedPermissionCode, "permissions_normalized_permission_code_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.NormalizedPermissionCode)
//                 .HasMaxLength(150)
//                 .HasComputedColumnSql("upper((permission_code)::text)", true)
//                 .HasColumnName("normalized_permission_code");
//             entity.Property(e => e.PermissionCode)
//                 .HasMaxLength(150)
//                 .HasColumnName("permission_code");
//         });
//
//         modelBuilder.Entity<ReviewImage>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("review_images_pkey");
//
//             entity.ToTable("review_images");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.ImageUrl)
//                 .HasMaxLength(500)
//                 .HasColumnName("image_url");
//             entity.Property(e => e.ReviewId).HasColumnName("review_id");
//             entity.Property(e => e.SortOrder)
//                 .HasDefaultValue(0)
//                 .HasColumnName("sort_order");
//
//             entity.HasOne(d => d.Review).WithMany(p => p.ReviewImages)
//                 .HasForeignKey(d => d.ReviewId)
//                 .HasConstraintName("review_images_review_id_fkey");
//         });
//
//         modelBuilder.Entity<ReviewReport>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("review_reports_pkey");
//
//             entity.ToTable("review_reports");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Description).HasColumnName("description");
//             entity.Property(e => e.Reason)
//                 .HasMaxLength(50)
//                 .HasColumnName("reason");
//             entity.Property(e => e.ReporterId).HasColumnName("reporter_id");
//             entity.Property(e => e.ReviewId).HasColumnName("review_id");
//             entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
//             entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'pending'::character varying")
//                 .HasColumnName("status");
//
//             entity.HasOne(d => d.Reporter).WithMany(p => p.ReviewReportReporters)
//                 .HasForeignKey(d => d.ReporterId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("review_reports_reporter_id_fkey");
//
//             entity.HasOne(d => d.Review).WithMany(p => p.ReviewReports)
//                 .HasForeignKey(d => d.ReviewId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("review_reports_review_id_fkey");
//
//             entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ReviewReportReviewedByNavigations)
//                 .HasForeignKey(d => d.ReviewedBy)
//                 .HasConstraintName("review_reports_reviewed_by_fkey");
//         });
//
//         modelBuilder.Entity<ReviewVote>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("review_votes_pkey");
//
//             entity.ToTable("review_votes");
//
//             entity.HasIndex(e => new { e.ReviewId, e.UserId }, "review_votes_review_id_user_id_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.IsHelpful).HasColumnName("is_helpful");
//             entity.Property(e => e.ReviewId).HasColumnName("review_id");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.Review).WithMany(p => p.ReviewVotes)
//                 .HasForeignKey(d => d.ReviewId)
//                 .HasConstraintName("review_votes_review_id_fkey");
//
//             entity.HasOne(d => d.User).WithMany(p => p.ReviewVotes)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("review_votes_user_id_fkey");
//         });
//
//         modelBuilder.Entity<Role>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("roles_pkey");
//
//             entity.ToTable("roles");
//
//             entity.HasIndex(e => e.NormalizedRoleName, "roles_normalized_role_name_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.NormalizedRoleName)
//                 .HasMaxLength(150)
//                 .HasComputedColumnSql("upper((role_name)::text)", true)
//                 .HasColumnName("normalized_role_name");
//             entity.Property(e => e.RoleName)
//                 .HasMaxLength(150)
//                 .HasColumnName("role_name");
//         });
//
//         modelBuilder.Entity<RolePermission>(entity =>
//         {
//             entity.HasKey(e => new { e.RoleId, e.PermissionId }).HasName("role_permissions_pkey");
//
//             entity.ToTable("role_permissions");
//
//             entity.Property(e => e.RoleId).HasColumnName("role_id");
//             entity.Property(e => e.PermissionId).HasColumnName("permission_id");
//             entity.Property(e => e.IsActive)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_active");
//
//             entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
//                 .HasForeignKey(d => d.PermissionId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("role_permissions_permission_id_fkey");
//
//             entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
//                 .HasForeignKey(d => d.RoleId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("role_permissions_role_id_fkey");
//         });
//
//         modelBuilder.Entity<SellerKyc>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("seller_kyc_pkey");
//
//             entity.ToTable("seller_kyc");
//
//             entity.HasIndex(e => e.IdNumber, "idx_seller_kyc_id_number");
//
//             entity.HasIndex(e => e.SellerProfileId, "idx_seller_kyc_seller");
//
//             entity.HasIndex(e => e.Status, "idx_seller_kyc_status");
//
//             entity.HasIndex(e => e.SubmittedAt, "idx_seller_kyc_submitted").HasFilter("((status)::text = 'submitted'::text)");
//
//             entity.HasIndex(e => e.SellerProfileId, "seller_kyc_seller_profile_id_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AttemptCount)
//                 .HasDefaultValue(0)
//                 .HasColumnName("attempt_count");
//             entity.Property(e => e.AutoVerified)
//                 .HasDefaultValue(false)
//                 .HasColumnName("auto_verified");
//             entity.Property(e => e.AutoVerifyProvider)
//                 .HasMaxLength(50)
//                 .HasColumnName("auto_verify_provider");
//             entity.Property(e => e.AutoVerifyResponse)
//                 .HasColumnType("jsonb")
//                 .HasColumnName("auto_verify_response");
//             entity.Property(e => e.AutoVerifyScore)
//                 .HasPrecision(5, 2)
//                 .HasColumnName("auto_verify_score");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
//             entity.Property(e => e.District)
//                 .HasMaxLength(100)
//                 .HasColumnName("district");
//             entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
//             entity.Property(e => e.FullName)
//                 .HasMaxLength(200)
//                 .HasColumnName("full_name");
//             entity.Property(e => e.Gender)
//                 .HasMaxLength(10)
//                 .HasColumnName("gender");
//             entity.Property(e => e.IdExpiredDate).HasColumnName("id_expired_date");
//             entity.Property(e => e.IdIssuedDate).HasColumnName("id_issued_date");
//             entity.Property(e => e.IdIssuedPlace)
//                 .HasMaxLength(200)
//                 .HasColumnName("id_issued_place");
//             entity.Property(e => e.IdNumber)
//                 .HasMaxLength(50)
//                 .HasColumnName("id_number");
//             entity.Property(e => e.IdType)
//                 .HasMaxLength(20)
//                 .HasColumnName("id_type");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.Nationality)
//                 .HasMaxLength(100)
//                 .HasDefaultValueSql("'Việt Nam'::character varying")
//                 .HasColumnName("nationality");
//             entity.Property(e => e.PermanentAddress).HasColumnName("permanent_address");
//             entity.Property(e => e.Province)
//                 .HasMaxLength(100)
//                 .HasColumnName("province");
//             entity.Property(e => e.RejectionCode)
//                 .HasMaxLength(50)
//                 .HasColumnName("rejection_code");
//             entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
//             entity.Property(e => e.SellerProfileId).HasColumnName("seller_profile_id");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'pending'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
//             entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
//             entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");
//             entity.Property(e => e.Ward)
//                 .HasMaxLength(100)
//                 .HasColumnName("ward");
//
//             entity.HasOne(d => d.SellerProfile).WithOne(p => p.SellerKyc)
//                 .HasForeignKey<SellerKyc>(d => d.SellerProfileId)
//                 .HasConstraintName("seller_kyc_seller_profile_id_fkey");
//
//             entity.HasOne(d => d.VerifiedByNavigation).WithMany(p => p.SellerKycs)
//                 .HasForeignKey(d => d.VerifiedBy)
//                 .HasConstraintName("seller_kyc_verified_by_fkey");
//         });
//
//         modelBuilder.Entity<SellerKycDocument>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("seller_kyc_documents_pkey");
//
//             entity.ToTable("seller_kyc_documents");
//
//             entity.HasIndex(e => e.KycId, "idx_kyc_documents_kyc");
//
//             entity.HasIndex(e => new { e.KycId, e.DocumentType }, "idx_kyc_documents_type");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.DocumentType)
//                 .HasMaxLength(30)
//                 .HasColumnName("document_type");
//             entity.Property(e => e.ExtractedData)
//                 .HasColumnType("jsonb")
//                 .HasColumnName("extracted_data");
//             entity.Property(e => e.FileHash)
//                 .HasMaxLength(64)
//                 .HasColumnName("file_hash");
//             entity.Property(e => e.FileName)
//                 .HasMaxLength(255)
//                 .HasColumnName("file_name");
//             entity.Property(e => e.FileSize).HasColumnName("file_size");
//             entity.Property(e => e.FileUrl)
//                 .HasMaxLength(500)
//                 .HasColumnName("file_url");
//             entity.Property(e => e.KycId).HasColumnName("kyc_id");
//             entity.Property(e => e.MimeType)
//                 .HasMaxLength(50)
//                 .HasColumnName("mime_type");
//             entity.Property(e => e.UploadedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("uploaded_at");
//             entity.Property(e => e.VerificationNotes).HasColumnName("verification_notes");
//             entity.Property(e => e.VerificationStatus)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'pending'::character varying")
//                 .HasColumnName("verification_status");
//             entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
//
//             entity.HasOne(d => d.Kyc).WithMany(p => p.SellerKycDocuments)
//                 .HasForeignKey(d => d.KycId)
//                 .HasConstraintName("seller_kyc_documents_kyc_id_fkey");
//         });
//
//         modelBuilder.Entity<SellerKycHistory>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("seller_kyc_history_pkey");
//
//             entity.ToTable("seller_kyc_history");
//
//             entity.HasIndex(e => e.CreatedAt, "idx_kyc_history_created");
//
//             entity.HasIndex(e => e.KycId, "idx_kyc_history_kyc");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Action)
//                 .HasMaxLength(30)
//                 .HasColumnName("action");
//             entity.Property(e => e.ChangedFields)
//                 .HasColumnType("jsonb")
//                 .HasColumnName("changed_fields");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.IpAddress).HasColumnName("ip_address");
//             entity.Property(e => e.KycId).HasColumnName("kyc_id");
//             entity.Property(e => e.NewStatus)
//                 .HasMaxLength(20)
//                 .HasColumnName("new_status");
//             entity.Property(e => e.Notes).HasColumnName("notes");
//             entity.Property(e => e.OldStatus)
//                 .HasMaxLength(20)
//                 .HasColumnName("old_status");
//             entity.Property(e => e.PerformedBy).HasColumnName("performed_by");
//             entity.Property(e => e.PerformedByType)
//                 .HasMaxLength(20)
//                 .HasColumnName("performed_by_type");
//             entity.Property(e => e.UserAgent).HasColumnName("user_agent");
//
//             entity.HasOne(d => d.Kyc).WithMany(p => p.SellerKycHistories)
//                 .HasForeignKey(d => d.KycId)
//                 .HasConstraintName("seller_kyc_history_kyc_id_fkey");
//
//             entity.HasOne(d => d.PerformedByNavigation).WithMany(p => p.SellerKycHistories)
//                 .HasForeignKey(d => d.PerformedBy)
//                 .HasConstraintName("seller_kyc_history_performed_by_fkey");
//         });
//
//         modelBuilder.Entity<SellerProfile>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("seller_profiles_pkey");
//
//             entity.ToTable("seller_profiles");
//
//             entity.HasIndex(e => e.Status, "idx_seller_profiles_status");
//
//             entity.Property(e => e.Id)
//                 .ValueGeneratedNever()
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(30)
//                 .HasDefaultValueSql("'pending'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.StoreDescription)
//                 .HasDefaultValueSql("'There are no description for this store.'::text")
//                 .HasColumnName("store_description");
//             entity.Property(e => e.StoreName)
//                 .HasMaxLength(200)
//                 .HasColumnName("store_name");
//             entity.Property(e => e.TotalSalesAmount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("total_sales_amount");
//             entity.Property(e => e.TotalSalesCount).HasColumnName("total_sales_count");
//             entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
//
//             entity.HasOne(d => d.IdNavigation).WithOne(p => p.SellerProfile)
//                 .HasForeignKey<SellerProfile>(d => d.Id)
//                 .HasConstraintName("seller_profiles_id_fkey");
//         });
//
//         modelBuilder.Entity<SellerRatingSummary>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("seller_rating_summary_pkey");
//
//             entity.ToTable("seller_rating_summary");
//
//             entity.HasIndex(e => e.SellerId, "seller_rating_summary_seller_id_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AverageRating)
//                 .HasPrecision(3, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("average_rating");
//             entity.Property(e => e.AvgCommunication)
//                 .HasPrecision(3, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("avg_communication");
//             entity.Property(e => e.AvgItemAccuracy)
//                 .HasPrecision(3, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("avg_item_accuracy");
//             entity.Property(e => e.AvgShippingSpeed)
//                 .HasPrecision(3, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("avg_shipping_speed");
//             entity.Property(e => e.LastUpdatedAt).HasColumnName("last_updated_at");
//             entity.Property(e => e.Rating1Count)
//                 .HasDefaultValue(0)
//                 .HasColumnName("rating_1_count");
//             entity.Property(e => e.Rating2Count)
//                 .HasDefaultValue(0)
//                 .HasColumnName("rating_2_count");
//             entity.Property(e => e.Rating3Count)
//                 .HasDefaultValue(0)
//                 .HasColumnName("rating_3_count");
//             entity.Property(e => e.Rating4Count)
//                 .HasDefaultValue(0)
//                 .HasColumnName("rating_4_count");
//             entity.Property(e => e.Rating5Count)
//                 .HasDefaultValue(0)
//                 .HasColumnName("rating_5_count");
//             entity.Property(e => e.ResponseCount)
//                 .HasDefaultValue(0)
//                 .HasColumnName("response_count");
//             entity.Property(e => e.SellerId).HasColumnName("seller_id");
//             entity.Property(e => e.TotalReviews)
//                 .HasDefaultValue(0)
//                 .HasColumnName("total_reviews");
//
//             entity.HasOne(d => d.Seller).WithOne(p => p.SellerRatingSummary)
//                 .HasForeignKey<SellerRatingSummary>(d => d.SellerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("seller_rating_summary_seller_id_fkey");
//         });
//
//         modelBuilder.Entity<SellerReview>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("seller_reviews_pkey");
//
//             entity.ToTable("seller_reviews");
//
//             entity.HasIndex(e => e.OrderId, "idx_seller_reviews_order");
//
//             entity.HasIndex(e => e.OverallRating, "idx_seller_reviews_rating");
//
//             entity.HasIndex(e => e.ReviewerId, "idx_seller_reviews_reviewer");
//
//             entity.HasIndex(e => e.SellerId, "idx_seller_reviews_seller");
//
//             entity.HasIndex(e => e.Status, "idx_seller_reviews_status");
//
//             entity.HasIndex(e => new { e.OrderId, e.ReviewerId }, "seller_reviews_order_id_reviewer_id_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AuctionId).HasColumnName("auction_id");
//             entity.Property(e => e.Comment).HasColumnName("comment");
//             entity.Property(e => e.CommunicationRating).HasColumnName("communication_rating");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.HelpfulCount)
//                 .HasDefaultValue(0)
//                 .HasColumnName("helpful_count");
//             entity.Property(e => e.IsVerifiedPurchase)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_verified_purchase");
//             entity.Property(e => e.ItemAccuracyRating).HasColumnName("item_accuracy_rating");
//             entity.Property(e => e.ModerationReason).HasColumnName("moderation_reason");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.NotHelpfulCount)
//                 .HasDefaultValue(0)
//                 .HasColumnName("not_helpful_count");
//             entity.Property(e => e.OrderId).HasColumnName("order_id");
//             entity.Property(e => e.OverallRating).HasColumnName("overall_rating");
//             entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");
//             entity.Property(e => e.SellerId).HasColumnName("seller_id");
//             entity.Property(e => e.SellerRespondedAt).HasColumnName("seller_responded_at");
//             entity.Property(e => e.SellerResponse).HasColumnName("seller_response");
//             entity.Property(e => e.ShippingSpeedRating).HasColumnName("shipping_speed_rating");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'published'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.Title)
//                 .HasMaxLength(200)
//                 .HasColumnName("title");
//
//             entity.HasOne(d => d.Auction).WithMany(p => p.SellerReviews)
//                 .HasForeignKey(d => d.AuctionId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("seller_reviews_auction_id_fkey");
//
//             entity.HasOne(d => d.Order).WithMany(p => p.SellerReviews)
//                 .HasForeignKey(d => d.OrderId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("seller_reviews_order_id_fkey");
//
//             entity.HasOne(d => d.Reviewer).WithMany(p => p.SellerReviewReviewers)
//                 .HasForeignKey(d => d.ReviewerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("seller_reviews_reviewer_id_fkey");
//
//             entity.HasOne(d => d.Seller).WithMany(p => p.SellerReviewSellers)
//                 .HasForeignKey(d => d.SellerId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("seller_reviews_seller_id_fkey");
//         });
//
//         modelBuilder.Entity<Shipment>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("shipments_pkey");
//
//             entity.ToTable("shipments");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.ActualDelivery).HasColumnName("actual_delivery");
//             entity.Property(e => e.Carrier)
//                 .HasMaxLength(50)
//                 .HasColumnName("carrier");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.EstimatedDelivery).HasColumnName("estimated_delivery");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.OrderId).HasColumnName("order_id");
//             entity.Property(e => e.ShippingFee)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("shipping_fee");
//             entity.Property(e => e.ShippingMethod)
//                 .HasMaxLength(50)
//                 .HasColumnName("shipping_method");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(30)
//                 .HasDefaultValueSql("'pending'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.TrackingNumber)
//                 .HasMaxLength(100)
//                 .HasColumnName("tracking_number");
//             entity.Property(e => e.WeightKg)
//                 .HasPrecision(8, 2)
//                 .HasColumnName("weight_kg");
//
//             entity.HasOne(d => d.Order).WithMany(p => p.Shipments)
//                 .HasForeignKey(d => d.OrderId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("shipments_order_id_fkey");
//         });
//
//         modelBuilder.Entity<Transaction>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("transactions_pkey");
//
//             entity.ToTable("transactions");
//
//             entity.HasIndex(e => e.OrderId, "idx_transactions_order");
//
//             entity.HasIndex(e => e.Status, "idx_transactions_status");
//
//             entity.HasIndex(e => e.UserId, "idx_transactions_user");
//
//             entity.HasIndex(e => e.TransactionNumber, "transactions_transaction_number_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Amount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("amount");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Currency)
//                 .HasMaxLength(3)
//                 .HasDefaultValueSql("'VND'::character varying")
//                 .HasColumnName("currency");
//             entity.Property(e => e.Description).HasColumnName("description");
//             entity.Property(e => e.Fee)
//                 .HasPrecision(18, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("fee");
//             entity.Property(e => e.GatewayProvider)
//                 .HasMaxLength(50)
//                 .HasColumnName("gateway_provider");
//             entity.Property(e => e.GatewayResponse)
//                 .HasColumnType("jsonb")
//                 .HasColumnName("gateway_response");
//             entity.Property(e => e.GatewayTransactionId)
//                 .HasMaxLength(255)
//                 .HasColumnName("gateway_transaction_id");
//             entity.Property(e => e.NetAmount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("net_amount");
//             entity.Property(e => e.OrderId).HasColumnName("order_id");
//             entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
//             entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'pending'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.TransactionNumber)
//                 .HasMaxLength(50)
//                 .HasColumnName("transaction_number");
//             entity.Property(e => e.Type)
//                 .HasMaxLength(20)
//                 .HasColumnName("type");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.Order).WithMany(p => p.Transactions)
//                 .HasForeignKey(d => d.OrderId)
//                 .HasConstraintName("transactions_order_id_fkey");
//
//             entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Transactions)
//                 .HasForeignKey(d => d.PaymentMethodId)
//                 .HasConstraintName("transactions_payment_method_id_fkey");
//
//             entity.HasOne(d => d.User).WithMany(p => p.Transactions)
//                 .HasForeignKey(d => d.UserId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("transactions_user_id_fkey");
//         });
//
//         modelBuilder.Entity<User>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("users_pkey");
//
//             entity.ToTable("users");
//
//             entity.HasIndex(e => e.NormalizedEmail, "idx_unique_users_normalized_email_active")
//                 .IsUnique()
//                 .HasFilter("(deleted_at IS NULL)");
//
//             entity.HasIndex(e => e.NormalizedUserName, "idx_unique_users_normalized_user_name_active")
//                 .IsUnique()
//                 .HasFilter("(deleted_at IS NULL)");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.AccessFailedCount).HasColumnName("access_failed_count");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
//             entity.Property(e => e.Email)
//                 .HasMaxLength(255)
//                 .HasColumnName("email");
//             entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
//             entity.Property(e => e.EmailConfirmedAt).HasColumnName("email_confirmed_at");
//             entity.Property(e => e.LockoutEnabled).HasColumnName("lockout_enabled");
//             entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.NormalizedEmail)
//                 .HasMaxLength(255)
//                 .HasComputedColumnSql("upper((email)::text)", true)
//                 .HasColumnName("normalized_email");
//             entity.Property(e => e.NormalizedUserName)
//                 .HasMaxLength(255)
//                 .HasComputedColumnSql("upper((user_name)::text)", true)
//                 .HasColumnName("normalized_user_name");
//             entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
//             entity.Property(e => e.PhoneNumber)
//                 .HasMaxLength(20)
//                 .HasColumnName("phone_number");
//             entity.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
//             entity.Property(e => e.PhoneNumberConfirmedAt).HasColumnName("phone_number_confirmed_at");
//             entity.Property(e => e.PhoneNumberCountryCode)
//                 .HasMaxLength(10)
//                 .IsFixedLength()
//                 .HasColumnName("phone_number_country_code");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(30)
//                 .HasDefaultValueSql("'inactive'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
//             entity.Property(e => e.TwoFactorProvider)
//                 .HasMaxLength(30)
//                 .HasDefaultValueSql("'none'::character varying")
//                 .HasColumnName("two_factor_provider");
//             entity.Property(e => e.UserName)
//                 .HasMaxLength(255)
//                 .HasColumnName("user_name");
//             entity.Property(e => e.Version).HasColumnName("version");
//         });
//
//         modelBuilder.Entity<UserAddress>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("user_addresses_pkey");
//
//             entity.ToTable("user_addresses");
//
//             entity.HasIndex(e => e.UserId, "idx_unique_default_address_per_user")
//                 .IsUnique()
//                 .HasFilter("(is_default = true)");
//
//             entity.HasIndex(e => e.UserId, "idx_user_addresses_user_id");
//
//             entity.HasIndex(e => new { e.UserId, e.Type }, "idx_user_addresses_user_id_type");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Address)
//                 .HasMaxLength(255)
//                 .HasColumnName("address");
//             entity.Property(e => e.City)
//                 .HasMaxLength(120)
//                 .HasColumnName("city");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.District)
//                 .HasMaxLength(100)
//                 .HasColumnName("district");
//             entity.Property(e => e.IsDefault).HasColumnName("is_default");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.PhoneNumber)
//                 .HasMaxLength(20)
//                 .HasColumnName("phone_number");
//             entity.Property(e => e.PostalCode)
//                 .HasMaxLength(10)
//                 .HasColumnName("postal_code");
//             entity.Property(e => e.RecipientName)
//                 .HasMaxLength(100)
//                 .HasColumnName("recipient_name");
//             entity.Property(e => e.Type)
//                 .HasMaxLength(10)
//                 .HasDefaultValueSql("'other'::character varying")
//                 .HasColumnName("type");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//             entity.Property(e => e.Ward)
//                 .HasMaxLength(100)
//                 .HasColumnName("ward");
//
//             entity.HasOne(d => d.User).WithOne(p => p.UserAddress)
//                 .HasForeignKey<UserAddress>(d => d.UserId)
//                 .HasConstraintName("user_addresses_user_id_fkey");
//         });
//
//         modelBuilder.Entity<UserLoginHistory>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("user_login_history_pkey");
//
//             entity.ToTable("user_login_history");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.IpAddress).HasColumnName("ip_address");
//             entity.Property(e => e.LoginAt).HasColumnName("login_at");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(30)
//                 .HasColumnName("status");
//             entity.Property(e => e.UserAgent).HasColumnName("user_agent");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.User).WithMany(p => p.UserLoginHistories)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("user_login_history_user_id_fkey");
//         });
//
//         modelBuilder.Entity<UserNotificationPreference>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("user_notification_preferences_pkey");
//
//             entity.ToTable("user_notification_preferences");
//
//             entity.HasIndex(e => e.UserId, "idx_preferences_user");
//
//             entity.HasIndex(e => e.UserId, "user_notification_preferences_user_id_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Channels)
//                 .HasDefaultValueSql("'{\"sms\": false, \"push\": true, \"email\": true}'::jsonb")
//                 .HasColumnType("jsonb")
//                 .HasColumnName("channels");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.IsEnabled)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_enabled");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.QuietHours)
//                 .HasColumnType("jsonb")
//                 .HasColumnName("quiet_hours");
//             entity.Property(e => e.RateLimits)
//                 .HasColumnType("jsonb")
//                 .HasColumnName("rate_limits");
//             entity.Property(e => e.TypePreferences)
//                 .HasDefaultValueSql("'{}'::jsonb")
//                 .HasColumnType("jsonb")
//                 .HasColumnName("type_preferences");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.User).WithOne(p => p.UserNotificationPreference)
//                 .HasForeignKey<UserNotificationPreference>(d => d.UserId)
//                 .HasConstraintName("user_notification_preferences_user_id_fkey");
//         });
//
//         modelBuilder.Entity<UserPermission>(entity =>
//         {
//             entity.HasKey(e => new { e.UserId, e.PermissionId }).HasName("user_permissions_pkey");
//
//             entity.ToTable("user_permissions");
//
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//             entity.Property(e => e.PermissionId).HasColumnName("permission_id");
//             entity.Property(e => e.IsAllowed)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_allowed");
//
//             entity.HasOne(d => d.Permission).WithMany(p => p.UserPermissions)
//                 .HasForeignKey(d => d.PermissionId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("user_permissions_permission_id_fkey");
//
//             entity.HasOne(d => d.User).WithMany(p => p.UserPermissions)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("user_permissions_user_id_fkey");
//         });
//
//         modelBuilder.Entity<UserProfile>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("user_profiles_pkey");
//
//             entity.ToTable("user_profiles");
//
//             entity.Property(e => e.Id)
//                 .ValueGeneratedNever()
//                 .HasColumnName("id");
//             entity.Property(e => e.AvatarUrl)
//                 .HasMaxLength(255)
//                 .HasColumnName("avatar_url");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
//             entity.Property(e => e.DisplayName)
//                 .HasMaxLength(100)
//                 .HasColumnName("display_name");
//             entity.Property(e => e.FirstName)
//                 .HasMaxLength(50)
//                 .HasColumnName("first_name");
//             entity.Property(e => e.Gender)
//                 .HasMaxLength(10)
//                 .HasColumnName("gender");
//             entity.Property(e => e.LastName)
//                 .HasMaxLength(50)
//                 .HasColumnName("last_name");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//
//             entity.HasOne(d => d.IdNavigation).WithOne(p => p.UserProfile)
//                 .HasForeignKey<UserProfile>(d => d.Id)
//                 .HasConstraintName("user_profiles_id_fkey");
//         });
//
//         modelBuilder.Entity<UserRefreshToken>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("user_refresh_tokens_pkey");
//
//             entity.ToTable("user_refresh_tokens");
//
//             entity.HasIndex(e => e.ExpiresAt, "ix_user_refresh_tokens_expires_at");
//
//             entity.HasIndex(e => new { e.FamilyId, e.CreatedAt }, "ix_user_refresh_tokens_family_family_id_created_at");
//
//             entity.HasIndex(e => e.FamilyId, "ix_user_refresh_tokens_family_id");
//
//             entity.HasIndex(e => new { e.UserId, e.IsUsed }, "ix_user_refresh_tokens_user_id_is_used");
//
//             entity.HasIndex(e => e.TokenHash, "user_refresh_tokens_token_hash_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.CreatedByIp).HasColumnName("created_by_ip");
//             entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
//             entity.Property(e => e.FamilyId).HasColumnName("family_id");
//             entity.Property(e => e.IsUsed).HasColumnName("is_used");
//             entity.Property(e => e.ParentTokenId).HasColumnName("parent_token_id");
//             entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
//             entity.Property(e => e.RevokedByIp).HasColumnName("revoked_by_ip");
//             entity.Property(e => e.RevokedReason).HasColumnName("revoked_reason");
//             entity.Property(e => e.RotationCounter).HasColumnName("rotation_counter");
//             entity.Property(e => e.TokenHash).HasColumnName("token_hash");
//             entity.Property(e => e.UsedAt).HasColumnName("used_at");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.Family).WithMany(p => p.UserRefreshTokens)
//                 .HasForeignKey(d => d.FamilyId)
//                 .HasConstraintName("user_refresh_tokens_family_id_fkey");
//
//             entity.HasOne(d => d.ParentToken).WithMany(p => p.InverseParentToken)
//                 .HasForeignKey(d => d.ParentTokenId)
//                 .OnDelete(DeleteBehavior.SetNull)
//                 .HasConstraintName("user_refresh_tokens_parent_token_id_fkey");
//
//             entity.HasOne(d => d.User).WithMany(p => p.UserRefreshTokens)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("user_refresh_tokens_user_id_fkey");
//         });
//
//         modelBuilder.Entity<UserRefreshTokenFamily>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("user_refresh_token_families_pkey");
//
//             entity.ToTable("user_refresh_token_families");
//
//             entity.HasIndex(e => e.UserId, "ix_user_refresh_token_families_user");
//
//             entity.HasIndex(e => new { e.UserId, e.IsActive }, "ix_user_refresh_token_families_user_active");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.DeviceId).HasColumnName("device_id");
//             entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
//             entity.Property(e => e.IpAddress).HasColumnName("ip_address");
//             entity.Property(e => e.IsActive)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_active");
//             entity.Property(e => e.LastRotatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("last_rotated_at");
//             entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
//             entity.Property(e => e.RevokedReason).HasColumnName("revoked_reason");
//             entity.Property(e => e.UserAgent).HasColumnName("user_agent");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//
//             entity.HasOne(d => d.User).WithMany(p => p.UserRefreshTokenFamilies)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("user_refresh_token_families_user_id_fkey");
//         });
//
//         modelBuilder.Entity<UserRole>(entity =>
//         {
//             entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("user_roles_pkey");
//
//             entity.ToTable("user_roles");
//
//             entity.HasIndex(e => e.RoleId, "ix_user_roles_role_id");
//
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//             entity.Property(e => e.RoleId).HasColumnName("role_id");
//             entity.Property(e => e.AssignedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("assigned_at");
//
//             entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
//                 .HasForeignKey(d => d.RoleId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("user_roles_role_id_fkey");
//
//             entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
//                 .HasForeignKey(d => d.UserId)
//                 .HasConstraintName("user_roles_user_id_fkey");
//         });
//
//         modelBuilder.Entity<Wallet>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("wallets_pkey");
//
//             entity.ToTable("wallets");
//
//             entity.HasIndex(e => e.UserId, "wallets_user_id_key").IsUnique();
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Balance)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("balance");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Currency)
//                 .HasMaxLength(3)
//                 .HasDefaultValueSql("'VND'::character varying")
//                 .HasColumnName("currency");
//             entity.Property(e => e.IsActive)
//                 .HasDefaultValue(true)
//                 .HasColumnName("is_active");
//             entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
//             entity.Property(e => e.PendingBalance)
//                 .HasPrecision(18, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("pending_balance");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//             entity.Property(e => e.Version).HasColumnName("version");
//
//             entity.HasOne(d => d.User).WithOne(p => p.Wallet)
//                 .HasForeignKey<Wallet>(d => d.UserId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("wallets_user_id_fkey");
//         });
//
//         modelBuilder.Entity<WalletTransaction>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("wallet_transactions_pkey");
//
//             entity.ToTable("wallet_transactions");
//
//             entity.HasIndex(e => e.WalletId, "idx_wallet_transactions_wallet");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Amount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("amount");
//             entity.Property(e => e.BalanceAfter)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("balance_after");
//             entity.Property(e => e.BalanceBefore)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("balance_before");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Description).HasColumnName("description");
//             entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
//             entity.Property(e => e.Type)
//                 .HasMaxLength(20)
//                 .HasColumnName("type");
//             entity.Property(e => e.WalletId).HasColumnName("wallet_id");
//
//             entity.HasOne(d => d.Transaction).WithMany(p => p.WalletTransactions)
//                 .HasForeignKey(d => d.TransactionId)
//                 .HasConstraintName("wallet_transactions_transaction_id_fkey");
//
//             entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
//                 .HasForeignKey(d => d.WalletId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("wallet_transactions_wallet_id_fkey");
//         });
//
//         modelBuilder.Entity<WithdrawalRequest>(entity =>
//         {
//             entity.HasKey(e => e.Id).HasName("withdrawal_requests_pkey");
//
//             entity.ToTable("withdrawal_requests");
//
//             entity.Property(e => e.Id)
//                 .HasDefaultValueSql("uuidv7()")
//                 .HasColumnName("id");
//             entity.Property(e => e.Amount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("amount");
//             entity.Property(e => e.BankAccountHolder)
//                 .HasMaxLength(200)
//                 .HasColumnName("bank_account_holder");
//             entity.Property(e => e.BankAccountNumber)
//                 .HasMaxLength(50)
//                 .HasColumnName("bank_account_number");
//             entity.Property(e => e.BankName)
//                 .HasMaxLength(100)
//                 .HasColumnName("bank_name");
//             entity.Property(e => e.CreatedAt)
//                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
//                 .HasColumnName("created_at");
//             entity.Property(e => e.Fee)
//                 .HasPrecision(18, 2)
//                 .HasDefaultValue(0m)
//                 .HasColumnName("fee");
//             entity.Property(e => e.NetAmount)
//                 .HasPrecision(18, 2)
//                 .HasColumnName("net_amount");
//             entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
//             entity.Property(e => e.ProcessedBy).HasColumnName("processed_by");
//             entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
//             entity.Property(e => e.Status)
//                 .HasMaxLength(20)
//                 .HasDefaultValueSql("'pending'::character varying")
//                 .HasColumnName("status");
//             entity.Property(e => e.UserId).HasColumnName("user_id");
//             entity.Property(e => e.WalletId).HasColumnName("wallet_id");
//
//             entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.WithdrawalRequestProcessedByNavigations)
//                 .HasForeignKey(d => d.ProcessedBy)
//                 .HasConstraintName("withdrawal_requests_processed_by_fkey");
//
//             entity.HasOne(d => d.User).WithMany(p => p.WithdrawalRequestUsers)
//                 .HasForeignKey(d => d.UserId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("withdrawal_requests_user_id_fkey");
//
//             entity.HasOne(d => d.Wallet).WithMany(p => p.WithdrawalRequests)
//                 .HasForeignKey(d => d.WalletId)
//                 .OnDelete(DeleteBehavior.ClientSetNull)
//                 .HasConstraintName("withdrawal_requests_wallet_id_fkey");
//         });
//
//         OnModelCreatingPartial(modelBuilder);
//     }
//
//     partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
// }
