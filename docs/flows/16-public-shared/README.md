# Flow 16 -- Public & Shared Endpoints

## Overview

Public/Shared endpoints serve two audiences:

| Audience | Description |
|----------|-------------|
| **Anonymous** | No authentication required -- any visitor can call these endpoints |
| **Any authenticated user** | Requires a valid JWT but no specific role or permission beyond what is noted |

These endpoints power storefront browsing, category navigation, seller discovery, legal/terms display, and item Q&A. They are read-heavy, cache-friendly, and form the foundation every client app depends on.

> **Note:** There is no public `GET /api/items` list endpoint. Items are discovered through auction listings (`GET /api/auctions`) or seller profile pages (`GET /api/sellers/{id}/items`).

---

## Endpoint Map

```mermaid
flowchart TB
    subgraph Categories["Categories (4 endpoints) -- Anonymous"]
        CAT_ALL["GET /api/categories"]
        CAT_ID["GET /api/categories/{id}"]
        CAT_SLUG["GET /api/categories/by-slug/{slug}"]
        CAT_CHILDREN["GET /api/categories/{id}/children"]
    end

    subgraph Auctions["Auctions (3 endpoints)"]
        AUC_LIST["GET /api/auctions\n(Anonymous)"]
        AUC_ID["GET /api/auctions/{id}\n(Anonymous)"]
        AUC_BIDS["GET /api/auctions/{id}/bids\n(Auth + ReadAutoBid)"]
    end

    subgraph Items["Items (1 endpoint) -- Anonymous"]
        ITEM_ID["GET /api/items/{id}"]
    end

    subgraph Sellers["Sellers (2 endpoints) -- Anonymous"]
        SELLER_PROFILE["GET /api/sellers/{id}"]
        SELLER_ITEMS["GET /api/sellers/{id}/items"]
    end

    subgraph Terms["Terms (2 endpoints) -- Anonymous"]
        TERMS_ALL["GET /api/terms/active"]
        TERMS_TYPE["GET /api/terms/{type}/active"]
    end

    subgraph QA["Item Q&A (3 endpoints) -- Authenticated"]
        QA_LIST["GET /api/items/{id}/questions"]
        QA_ASK["POST /api/items/{id}/questions\n(AskQuestion permission)"]
        QA_ANSWER["POST /api/items/{id}/questions/{qid}/answer\n(Seller only)"]
    end

    CAT_ALL --> CAT_ID
    CAT_ALL --> CAT_SLUG
    CAT_ID --> CAT_CHILDREN

    AUC_LIST --> AUC_ID
    AUC_ID --> AUC_BIDS

    AUC_ID --> ITEM_ID

    SELLER_PROFILE --> SELLER_ITEMS
    SELLER_ITEMS --> ITEM_ID

    QA_ASK --> QA_LIST
    QA_ANSWER --> QA_LIST
```

---

## Full Endpoint Table

| # | Route | Method | Auth | Response |
|---|-------|--------|------|----------|
| 1 | `api/categories` | GET | Anonymous | `200` Paged `CategoryDto[]` |
| 2 | `api/categories/{categoryId}` | GET | Anonymous | `200` `CategoryDto` / `404` |
| 3 | `api/categories/by-slug/{slug}` | GET | Anonymous | `200` `CategoryDto` / `404` |
| 4 | `api/categories/{categoryId}/children` | GET | Anonymous | `200` Paged `CategoryDto[]` |
| 5 | `api/auctions` | GET | Anonymous | `200` Paged `AuctionDto[]` |
| 6 | `api/auctions/{auctionId}` | GET | Anonymous | `200` `AuctionDetailDto` / `404` |
| 7 | `api/auctions/{auctionId}/bids` | GET | Auth + `ReadAutoBid` | `200` Paged `BidDto[]` / `404` |
| 8 | `api/items/{itemId}` | GET | Anonymous | `200` `ItemDto` |
| 9 | `api/sellers/{sellerId}` | GET | Anonymous | `200` `PublicSellerProfileDto` |
| 10 | `api/sellers/{sellerId}/items` | GET | Anonymous | `200` Paged `PublicSellerItemDto[]` |
| 11 | `api/terms/active` | GET | Anonymous | `200` `TermsDocumentDto[]` |
| 12 | `api/terms/{type}/active` | GET | Anonymous | `200` `TermsDocumentDto` / `404` |
| 13 | `api/items/{itemId}/questions` | GET | Authenticated | `200` Paged `ItemQuestionDto[]` / `404` |
| 14 | `api/items/{itemId}/questions` | POST | Auth + `AskQuestion` | `201` Created / `422` |
| 15 | `api/items/{itemId}/questions/{questionId}/answer` | POST | Authenticated (seller) | `204` / `404` |

---

## Subflow Index

| File | Domain | Endpoints |
|------|--------|-----------|
| [01-categories.md](01-categories.md) | Category tree navigation | 4 |
| [02-auction-browsing.md](02-auction-browsing.md) | Auction listing & detail | 3 |
| [03-item-browsing.md](03-item-browsing.md) | Single item detail | 1 |
| [04-seller-public-profile.md](04-seller-public-profile.md) | Seller storefront | 2 |
| [05-terms.md](05-terms.md) | Legal terms & conditions | 2 |
| [06-item-qa.md](06-item-qa.md) | Item questions & answers | 3 |

---

## Key Source Files

| Area | Path |
|------|------|
| Category endpoints | `src/presentation/OIO.Api/Endpoints/AuctionContext/Categories/` |
| Auction endpoints | `src/presentation/OIO.Api/Endpoints/AuctionContext/Auctions/` |
| Item endpoints | `src/presentation/OIO.Api/Endpoints/AuctionContext/Items/` |
| Seller endpoints | `src/presentation/OIO.Api/Endpoints/UserContext/Sellers/` |
| Terms endpoints | `src/presentation/OIO.Api/Endpoints/UserContext/Terms/` |
| URL constants | `src/presentation/OIO.Api/Common/ApiEndpoint.Url.cs` |
