namespace OIO.Infrastructure.Settings;

public class ElasticsearchSettings
{
    public const string SectionName = "Elasticsearch";

    public string Endpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    
    public string AuctionsIndex { get; init; } = "oio_auctions";
    public string ItemsIndex { get; init; } = "oio_items";
    public string UsersIndex { get; init; } = "oio_users";
    public string OrdersIndex { get; init; } = "oio_orders";
    public string ShipmentsIndex { get; init; } = "oio_shipments";
    public string WarehouseIndex { get; init; } = "oio_warehouse";
}
