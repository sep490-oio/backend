# 03 -- Item Questions & Answers

## Overview

Buyers can ask questions about items, and sellers can answer them. Questions are public by default and tied to the item aggregate. The system enforces a configurable maximum number of questions per item (`MaxQuestionsPerItem`, default 100) and prevents sellers from asking questions on their own items.

Questions can only be asked on items that are **not** in `Draft` or `Removed` status.

---

## Endpoints

### 1. POST `/api/items/{itemId}/questions` -- Ask a Question

**Auth:** `items.ask_question`

Allows an authenticated user (buyer) to ask a question on an item.

**Path parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `itemId` | `guid` | Target item |

**Request body:**

```json
{
  "question": "Is the original box included?"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `question` | `string` | Yes | Not whitespace, max **1000** characters |

**Handler flow:**

1. Load item with public questions (`IsPublic == true`, ordered by `CreatedAt` descending).
2. Call `item.AskQuestion(askerId, question, maxQuestionsPerItem, nowUtc)`.
3. Domain validates:
   - Item status is not `Draft` or `Removed`.
   - Asker is not the seller (`askerId != SellerId`).
   - Question count has not reached `MaxQuestionsPerItem` (configurable, default: **100**).
4. Creates `ItemQuestion` entity with `IsPublic = true` by default.
5. Raises `ItemQuestionAskedEvent` with `ItemId`, `QuestionId`, `AskerId`.
6. Save and return DTO.

**Response:** `201 Created`

```json
{
  "id": "3fa85f64-...",
  "askerId": "3fa85f64-...",
  "question": "Is the original box included?",
  "answer": null,
  "answeredAt": null,
  "isPublic": true,
  "createdAt": "2026-03-20T10:00:00Z"
}
```

---

### 2. POST `/api/items/{itemId}/questions/{questionId}/answer` -- Answer a Question

**Auth:** Authenticated (seller ownership enforced in handler)

Allows the item's seller to answer a previously asked question.

**Path parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `itemId` | `guid` | Target item |
| `questionId` | `guid` | Question to answer |

**Request body:**

```json
{
  "answer": "Yes, it comes with the original box and papers."
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `answer` | `string` | Yes | Not whitespace, max **2000** characters |

**Handler flow:**

1. Load item with public questions.
2. Verify current user is the item's seller (`item.SellerId == currentUser.UserId`).
3. Call `item.AnswerQuestion(questionId, answer, nowUtc)`.
4. Domain validates:
   - Question exists on the item.
   - Question has not already been answered (`IsAnswered == false`).
5. Sets `Answer` and `AnsweredAt` on the question entity.
6. Raises `ItemQuestionAnsweredEvent` with `ItemId`, `QuestionId`, `AskerId`.
7. Save.

**Response:** `204 No Content`

---

### 3. GET `/api/items/{itemId}/questions` -- Get Public Item Questions

**Auth:** Authenticated

Returns a paginated list of public questions for the specified item.

**Path parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `itemId` | `guid` | Target item |

**Query parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | `int` | 1 | Page number |
| `pageSize` | `int` | 20 | Items per page |
| `sortBy` | `string?` | - | Sort expression (validated against allowed mappings) |

**Response:** `200 OK` with `PagedList<ItemQuestionDto>`

---

## ItemQuestionDto Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Question ID |
| `askerId` | `Guid` | User who asked the question |
| `question` | `string` | Question text (max 1000 chars) |
| `answer` | `string?` | Seller's answer (max 2000 chars), null if unanswered |
| `answeredAt` | `DateTime?` | Timestamp when answered, null if unanswered |
| `isPublic` | `bool` | Whether the question is publicly visible (default: `true`) |
| `createdAt` | `DateTime` | When the question was asked |

---

## Notifications

| Trigger | Event Type | Recipient | Description |
|---------|-----------|-----------|-------------|
| Question asked | `ItemQuestionAskedEvent` | Seller | Domain event raised; downstream handlers can dispatch notification to the item seller |
| Question answered | `ItemQuestionAnsweredEvent` | Asker | Domain event raised; downstream handlers can dispatch notification to the original asker |

---

## Configuration

| Setting | Location | Default | Description |
|---------|----------|---------|-------------|
| `Item.MaxQuestionsPerItem` | `appsettings.json` > `Item` | 100 | Maximum number of questions allowed per item |

---

## Error Codes

| Code | Type | Condition |
|------|------|-----------|
| `Item.NotFound` | 404 | Item with given ID not found |
| `Item.NotOwnedByUser` | 403 | Current user is not the seller (answer only) |
| `Item.InvalidState` | 409 | Item is in `Draft` or `Removed` status (cannot ask questions) |
| `Item.AskOwnItem` | 403 | Seller attempted to ask a question on their own item |
| `Item.QuestionLimitReached` | 409 | Item has reached `MaxQuestionsPerItem` |
| `Item.Question.NotFound` | 404 | Question with given ID not found on this item |
| `Item.Question.Answered` | 409 | Question has already been answered |

---

## Source Files

- Domain entity: `src/core/OIO.Domain/Context/CatalogContext/Aggregates/Items/ItemQuestion.cs`
- Domain methods: `src/core/OIO.Domain/Context/CatalogContext/Aggregates/Items/Item.cs` (`AskQuestion`, `AnswerQuestion`)
- Command (Ask): `src/core/OIO.Application/Context/AuctionContext/Commands/AskQuestion/AskQuestionCommand.cs`
- Command (Answer): `src/core/OIO.Application/Context/AuctionContext/Commands/AnswerQuestion/AnswerQuestionCommand.cs`
- Query: `src/core/OIO.Application/Context/AuctionContext/Queries/GetPublicItemQuestions/GetItemQuestionsQuery.cs`
- Endpoints: `src/presentation/OIO.Api/Endpoints/AuctionContext/Items/AskQuestionEndpoint.cs`, `AnswerQuestionEndpoint.cs`, `GetPublicItemQuestionsEndpoint.cs`
- Events: `src/core/OIO.Domain/Context/CatalogContext/Aggregates/Items/Events/ItemCreatedEvent.cs` (contains `ItemQuestionAskedEvent`, `ItemQuestionAnsweredEvent`)
- DTO: `src/core/OIO.Application/Context/AuctionContext/DTOs/ItemQuestionDto.cs`
