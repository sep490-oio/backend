using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class TwoFactorProvider : EnumValueObject<TwoFactorProvider>
{
    public static readonly TwoFactorProvider None = new("none");
    public static readonly TwoFactorProvider Sms = new("sms");
    public static readonly TwoFactorProvider Email = new("email");
    
    private TwoFactorProvider(string id) : base(id)
    {
    }
}

public sealed class UserStatus : EnumValueObject<UserStatus>
{
    public static readonly UserStatus Active = new("active");
    public static readonly UserStatus Inactive = new("inactive");
    public static readonly UserStatus Banned = new("banned");
    public static readonly UserStatus Suspended = new("suspended");
    
    private UserStatus (string id) : base(id)
    {
    }
}

public sealed class LoginStatus : EnumValueObject<LoginStatus>
{
    public static readonly LoginStatus Success =  new("success");
    public static readonly LoginStatus Failed =  new("failed");
    
    public LoginStatus(string id) : base(id)
    {
    }
}

public sealed class AddressType : EnumValueObject<AddressType>
{
    public static readonly AddressType Home =  new("home");
    public static readonly AddressType Work = new("work");
    public static readonly AddressType Other = new("other");
    
    public AddressType(string id) : base(id)
    {
    }
}

public sealed class Gender : EnumValueObject<Gender>
{
    public static readonly Gender Male = new("male");
    public static readonly Gender Female = new("female");
    public static readonly Gender Other = new("other");
    private Gender(string id) : base(id) {}
}

public sealed class SellerProfileStatus : EnumValueObject<SellerProfileStatus>
{
    public static readonly SellerProfileStatus Pending =  new("pending");
    public static readonly SellerProfileStatus Verified = new("verified");
    public static readonly SellerProfileStatus Rejected = new("rejected");
    
    public SellerProfileStatus(string id) : base(id)
    {
    }
}

public sealed class SellerKycStatus : EnumValueObject<SellerKycStatus>
{
    public static readonly SellerKycStatus Pending = new("pending");
    public static readonly SellerKycStatus Submitted = new("submitted");
    public static readonly SellerKycStatus UnderReview = new("under_review");
    public static readonly SellerKycStatus Approved = new("approved");
    public static readonly SellerKycStatus Rejected = new("rejected");
    public static readonly SellerKycStatus Expired = new("expired");
    public static readonly SellerKycStatus Suspended = new("suspended");
  
    public SellerKycStatus(string id) : base(id)
    {
    }
}

public sealed class KycIdType : EnumValueObject<KycIdType>
{
    public static readonly KycIdType Cccd = new("cccd");
    public static readonly KycIdType Cmnd = new("cmnd");
    public static readonly KycIdType Passport = new("passport");
        
    public KycIdType(string id) : base(id)
    {
    }
}

public sealed class KycDocumentType : EnumValueObject<KycDocumentType>
{
    public static KycDocumentType IdFront = new ("id_front");
    public static KycDocumentType IdBack = new ("id_back");
    public static KycDocumentType Selfie = new ("selfie");
    public static KycDocumentType SelfieWithId = new ("selfie_with_id");
    public static KycDocumentType BusinessLicense = new ("business_license");
    public static KycDocumentType BankStatement = new ("bank_statement");
    public static KycDocumentType Other = new ("other");
    
    public KycDocumentType(string id) : base(id)
    {
    }
}

public sealed class KycVerificationStatus : EnumValueObject<KycVerificationStatus>
{
    public static KycVerificationStatus Pending = new("pending");
    public static KycVerificationStatus Valid = new("valid");
    public static KycVerificationStatus Invalid = new("invalid");
    public static KycVerificationStatus Unclear = new("unclear");
    
    public KycVerificationStatus(string id) : base(id) {}
}

public sealed class SellerKycHistoryAction : EnumValueObject<SellerKycHistoryAction>
{
    public static SellerKycHistoryAction Created = new("created");
    public static SellerKycHistoryAction Submitted = new("submitted");
    public static SellerKycHistoryAction AutoVerified = new("auto_verified");
    public static SellerKycHistoryAction ManualReviewStarted = new("manual_review_started");
    public static SellerKycHistoryAction Approved = new("approved");
    public static SellerKycHistoryAction Rejected = new("rejected");
    public static SellerKycHistoryAction ReSubmitted = new("resubmitted");
    public static SellerKycHistoryAction Expired = new("expired");
    public static SellerKycHistoryAction Suspended = new("suspended");
    public static SellerKycHistoryAction DocumentUploaded = new("document_uploaded");
    public static SellerKycHistoryAction DocumentDeleted = new("document_deleted");
    public static SellerKycHistoryAction InfoUpdated = new("info_updated");
    
    public SellerKycHistoryAction(string id) : base(id) {}
}

public sealed class KycHistoryPerformedByType : EnumValueObject<KycHistoryPerformedByType>
{
    public static readonly KycHistoryPerformedByType Admin = new("admin");
    public static readonly KycHistoryPerformedByType System = new("system");
    public static readonly KycHistoryPerformedByType Seller =  new("seller");
    
    public KycHistoryPerformedByType(string id) : base(id) {}
}