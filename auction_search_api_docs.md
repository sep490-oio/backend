# Auction Search API Documentation (Internal)

**Endpoint**: `GET {BASE_URL}/api/search/auctions`

## 1. Query Parameters

| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `q` | string | No | Search keyword. Defaults to `*`. |
| `page` | int | No | Page number (1-based). Default: `1`. |
| `page_size` | int | No | Items per page. Default: `10`. |
| `sort_by` | string | No | Sort key (see list below). Default: `newest`. |
| `desc` | bool | No | Sort direction. Default: `true`. |
| `status` | string | No | Auction status filter (`Active`, `Scheduled`, etc.). |
| `category` | string | No | Category name filter. |
| `auction_type`| string | No | `Regular` or `Sealed`. |
| `condition` | string | No | Item physical condition (`New`, `LikeNew`, `VeryGood`, `Good`, `Acceptable`). |
| `min_price` | decimal | No | Minimum current price. |
| `max_price` | decimal | No | Maximum current price. |

## 2. Sort Keys (`sort_by`)

Pass these literal strings to the `sort_by` parameter:

| Value | Sort Logic |
| :--- | :--- |
| `ending_soon` | Time remaining (EndTime ASC) |
| `price_asc` | Price Low to High |
| `price_desc` | Price High to Low |
| `most_bids` | Most bids (BidCount DESC) |
| `newest` | Creation date DESC |

## 3. Response Structure: Facets

The response includes a `Facets` array. Use this to render sidebar filters with counts.

```json
{
  "results": [...],
  "total": 120,
  "page": 1,
  "pageSize": 10,
  "facets": [
    {
      "name": "Auction Types",
      "buckets": [
        { "key": "Regular", "count": 100 },
        { "key": "Sealed", "count": 20 }
      ]
    },
    {
      "name": "Conditions",
      "buckets": [
        { "key": "New", "count": 50 },
        { "key": "LikeNew", "count": 10 }
      ]
    },
    { "name": "Categories", "buckets": [...] },
    { "name": "Statuses", "buckets": [...] }
  ]
}
```

## 4. Integration Example

**Scenario**: Search for "Laptop", new condition, price under $2000, sort by ending soon.

`GET /api/search/auctions?q=laptop&condition=New&max_price=2000&sort_by=ending_soon`

---

## 5. UI Implementation Guidelines (For Frontend AI)

When generating the User Interface, follow these strict rules to ensure data synchronization:

1.  **Filter State Persistent**: Always maintain a global state for all active filters (Category, Status, Auction Type, Condition, Price Range, and Sort).
2.  **Cumulative Filtering**: When the user types a keyword in the Search Bar and presses "Search", DO NOT clear existing filters. Construct the API request with the current `q` keyword PLUS all active filter values.
3.  **Auto-Refresh on Filter**: Any change in the Sidebar Filters (e.g., clicking "Regular" in Auction Type) should immediately trigger a new API call with all current parameters to update the results.
4.  **Pagination Safety**: Changing any filter or sorting option must reset the `page` parameter to `1`.
5.  **Synchronization**: The "Search" button and any "Filter" buttons must share the same request-building logic. 
6.  **Sort Mapping**: The UI sorting dropdown MUST use the exact string keys defined in Section 2 (e.g., `ending_soon`, `price_asc`). Do not use custom keys; map the display labels (e.g., "Ending Soon") directly to these API-compliant values.
7.  **Price Range UI**: Implement two numeric input fields for "Min Price" and "Max Price". 
    *   **Trigger**: Since there is no "Apply" button, use a **Debounce** (e.g., 500ms-800ms) or trigger the search on **Blur** (when the user clicks outside the input). This prevents firing multiple API requests for every digit typed.
    *   **Validation**: Ensure that the `min_price` is always less than or equal to `max_price` before sending the request.
8.  **URL Synchronization**: The UI must synchronize the current search state (keyword, filters, sort, page) with the browser's URL Query Parameters. 
    *   This enables the browser's **Back/Forward** buttons to work correctly.
    *   It allows users to **share or bookmark** specific filtered search results by simply copying the URL.

**Example Logic**:
```typescript
// When Search is triggered
const searchUrl = `/api/search/auctions?q=${keyword}&auction_type=${currentSelectedType}&condition=${currentSelectedCondition}&min_price=${minPrice}&max_price=${maxPrice}&sort_by=${currentSort}`;
// Fetch data using searchUrl...
```
