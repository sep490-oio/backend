using System.Text.Json.Serialization;

namespace OIO.Infrastructure.Shipping.Ghn;

// ============================================================
// Credentials
// Stored in ShippingProviderConfig.Credentials as jsonb.
// GHN sandbox: { "token": "672e7794-...", "shop_id": 199494, "client_id": 2511126 }
// ============================================================

internal sealed class GhnCredentials
{
    [JsonPropertyName("token")]     public required string Token    { get; init; }
    [JsonPropertyName("shop_id")]   public required int    ShopId   { get; init; }
    [JsonPropertyName("client_id")] public          int    ClientId { get; init; }
}

// ============================================================
// Create Order
// POST /shiip/public-api/v2/shipping-order/create
// Headers: Token + ShopId
// ============================================================

internal sealed class GhnCreateOrderRequest
{
    [JsonPropertyName("payment_type_id")]  public required int    PaymentTypeId  { get; init; } // 1=shop pays, 2=buyer pays
    [JsonPropertyName("required_note")]    public required string RequiredNote   { get; init; } // CHOTHUHANG|CHOXEMHANGKHONGTHU|KHONGCHOXEMHANG
    [JsonPropertyName("client_order_code")] public required string ClientOrderCode { get; init; }

    // Recipient address — GHN uses IDs, not plain text
    [JsonPropertyName("to_name")]        public required string ToName       { get; init; }
    [JsonPropertyName("to_phone")]       public required string ToPhone      { get; init; }
    [JsonPropertyName("to_address")]     public required string ToAddress    { get; init; }
    [JsonPropertyName("to_ward_code")]   public required string ToWardCode   { get; init; }  // string e.g. "21012"
    [JsonPropertyName("to_district_id")] public required int    ToDistrictId { get; init; }  // int e.g. 1442
    [JsonPropertyName("from_name")]          public string? FromName         { get; init; }
    [JsonPropertyName("from_phone")]         public string? FromPhone        { get; init; }
    [JsonPropertyName("from_address")]       public string? FromAddress      { get; init; }
    [JsonPropertyName("from_ward_name")]     public string? FromWardName     { get; init; }
    [JsonPropertyName("from_district_name")] public string? FromDistrictName { get; init; }
    // Package — GHN uses grams for weight, cm for dimensions
    [JsonPropertyName("weight")] public required int  Weight { get; init; }  // grams
    [JsonPropertyName("length")] public          int? Length { get; init; }  // cm
    [JsonPropertyName("width")]  public          int? Width  { get; init; }  // cm
    [JsonPropertyName("height")] public          int? Height { get; init; }  // cm

    // Financials
    [JsonPropertyName("insurance_value")] public decimal InsuranceValue { get; init; }
    [JsonPropertyName("cod_amount")]      public decimal CodAmount      { get; init; }

    // Service — default 2 = standard express
    [JsonPropertyName("service_type_id")] public int ServiceTypeId { get; init; } = 2;

    // Items (required)
    [JsonPropertyName("items")] public required List<GhnOrderItem> Items { get; init; }
}

internal sealed class GhnOrderItem
{
    [JsonPropertyName("name")]     public required string Name     { get; init; }
    [JsonPropertyName("code")]     public required string Code     { get; init; }
    [JsonPropertyName("quantity")] public required int    Quantity { get; init; }
    [JsonPropertyName("price")]    public required int    Price    { get; init; }  // VND, integer
    [JsonPropertyName("weight")]   public required int    Weight   { get; init; }  // grams
    [JsonPropertyName("length")]   public          int?   Length   { get; init; }
    [JsonPropertyName("width")]    public          int?   Width    { get; init; }
    [JsonPropertyName("height")]   public          int?   Height   { get; init; }
}

internal sealed class GhnCreateOrderResponse
{
    [JsonPropertyName("code")]    public int    Code    { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = "";
    [JsonPropertyName("data")]    public GhnCreateOrderData? Data { get; init; }
}

internal sealed class GhnCreateOrderData
{
    [JsonPropertyName("order_code")]            public string  OrderCode           { get; init; } = "";  // GHN tracking number
    [JsonPropertyName("sort_code")]             public string? SortCode            { get; init; }
    [JsonPropertyName("trans_type")]            public string? TransType           { get; init; }
    [JsonPropertyName("ward_encode")]           public string? WardEncode          { get; init; }
    [JsonPropertyName("district_encode")]       public string? DistrictEncode      { get; init; }
    [JsonPropertyName("fee")]                   public GhnFeeDetail? Fee           { get; init; }
    [JsonPropertyName("total_fee")]             public int     TotalFee            { get; init; }  // VND
    [JsonPropertyName("expected_delivery_time")] public string? ExpectedDeliveryTime { get; init; } // ISO 8601
}

internal sealed class GhnFeeDetail
{
    [JsonPropertyName("main_service")]  public int MainService  { get; init; }
    [JsonPropertyName("insurance")]     public int Insurance    { get; init; }
    [JsonPropertyName("cod_fee")]       public int CodFee       { get; init; }
    [JsonPropertyName("station_do")]    public int StationDo    { get; init; }
    [JsonPropertyName("station_pu")]    public int StationPu    { get; init; }
    [JsonPropertyName("return")]        public int Return       { get; init; }
    [JsonPropertyName("r2s")]           public int R2S          { get; init; }
    [JsonPropertyName("return_again")]  public int ReturnAgain  { get; init; }
}

// ============================================================
// Cancel Order
// POST /shiip/public-api/v2/switch-status/cancel
// Body: { "order_codes": ["XXXXXXX"] }
// ============================================================

internal sealed class GhnCancelOrderRequest
{
    [JsonPropertyName("order_codes")] public required List<string> OrderCodes { get; init; }
}

internal sealed class GhnCancelOrderResponse
{
    [JsonPropertyName("code")]    public int    Code    { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = "";
    [JsonPropertyName("data")]    public List<GhnCancelResult>? Data { get; init; }
}

internal sealed class GhnCancelResult
{
    [JsonPropertyName("order_code")] public string OrderCode { get; init; } = "";
    [JsonPropertyName("result")]     public bool   Result    { get; init; }
    [JsonPropertyName("message")]    public string? Message  { get; init; }
}

// ============================================================
// Calculate Fee
// POST /shiip/public-api/v2/shipping-order/fee
// ============================================================

internal sealed class GhnCalculateFeeRequest
{
    [JsonPropertyName("service_type_id")] public int    ServiceTypeId { get; init; } = 2;
    [JsonPropertyName("to_ward_code")]    public required string ToWardCode   { get; init; }
    [JsonPropertyName("to_district_id")]  public required int    ToDistrictId { get; init; }
    [JsonPropertyName("weight")]          public required int    Weight       { get; init; }  // grams
    [JsonPropertyName("length")]          public          int?   Length       { get; init; }
    [JsonPropertyName("width")]           public          int?   Width        { get; init; }
    [JsonPropertyName("height")]          public          int?   Height       { get; init; }
    [JsonPropertyName("insurance_value")] public          decimal InsuranceValue { get; init; }
    [JsonPropertyName("coupon")]          public          string? Coupon      { get; init; }
}

internal sealed class GhnCalculateFeeResponse
{
    [JsonPropertyName("code")]    public int    Code    { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = "";
    [JsonPropertyName("data")]    public GhnCalculateFeeData? Data { get; init; }
}

internal sealed class GhnCalculateFeeData
{
    [JsonPropertyName("total")]            public int Total           { get; init; }  // VND total fee
    [JsonPropertyName("service_fee")]      public int ServiceFee      { get; init; }
    [JsonPropertyName("insurance_fee")]    public int InsuranceFee    { get; init; }
    [JsonPropertyName("pick_station_fee")] public int PickStationFee  { get; init; }
    [JsonPropertyName("coupon_value")]     public int CouponValue     { get; init; }
    [JsonPropertyName("r2s_fee")]          public int R2SFee          { get; init; }
    [JsonPropertyName("return_again")]     public int ReturnAgain     { get; init; }
    [JsonPropertyName("document_return")]  public int DocumentReturn  { get; init; }
    [JsonPropertyName("double_check")]     public int DoubleCheck     { get; init; }
    [JsonPropertyName("cod_fee")]          public int CodFee          { get; init; }
    [JsonPropertyName("pick_shift_fee")]   public int PickShiftFee    { get; init; }
}

// ============================================================
// Webhook Payload
// GHN pushes JSON. No HMAC — verify ShopId in payload.
// ============================================================

internal sealed class GhnWebhookPayload
{
    // Identifies which shop this event belongs to — verify against our config
    [JsonPropertyName("ShopID")]         public int    ShopId        { get; init; }
    [JsonPropertyName("ClientOrderCode")] public string? ClientOrderCode { get; init; }  // our ref
    [JsonPropertyName("OrderCode")]      public string  OrderCode     { get; init; } = "";  // GHN tracking number
    [JsonPropertyName("Status")]         public string  Status        { get; init; } = "";  // e.g. "delivered"
    [JsonPropertyName("ExtraInformation")] public string? ExtraInformation { get; init; }
    [JsonPropertyName("Description")]    public string? Description   { get; init; }
    [JsonPropertyName("Reason")]         public string? Reason        { get; init; }
    [JsonPropertyName("Time")]           public string? Time          { get; init; }       // ISO 8601
    [JsonPropertyName("Timestamp")]      public long?   Timestamp     { get; init; }       // Unix seconds fallback
    [JsonPropertyName("Warehouse")]      public string? Warehouse     { get; init; }       // location/hub name
    [JsonPropertyName("WarehouseAddress")] public string? WarehouseAddress { get; init; }
    [JsonPropertyName("CODAmount")]      public decimal? CodAmount    { get; init; }
    [JsonPropertyName("CODTransferDate")] public string? CodTransferDate { get; init; }
}

// ============================================================
// Expected Delivery Time
// POST /shiip/public-api/v2/shipping-order/leadtime
// ============================================================

internal sealed class GhnExpectedDeliveryTimeRequest
{
    [JsonPropertyName("from_district_id")] public required int FromDistrictId { get; init; }
    [JsonPropertyName("from_ward_code")]   public required string FromWardCode { get; init; }
    [JsonPropertyName("to_district_id")]   public required int ToDistrictId { get; init; }
    [JsonPropertyName("to_ward_code")]     public required string ToWardCode { get; init; }
    [JsonPropertyName("service_id")]       public int ServiceId { get; init; } = 53320; // Default Standard Express
}

internal sealed class GhnExpectedDeliveryTimeResponse
{
    [JsonPropertyName("code")]    public int    Code    { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = "";
    [JsonPropertyName("data")]    public GhnExpectedDeliveryTimeData? Data { get; init; }
}

internal sealed class GhnExpectedDeliveryTimeData
{
    [JsonPropertyName("leadtime")]   public long Leadtime { get; init; } // Unix timestamp
    [JsonPropertyName("order_date")] public long OrderDate { get; init; } // Unix timestamp
}