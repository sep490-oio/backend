# 01 - Registration (Dang ky tai khoan)

## Endpoint

| Thuoc tinh | Gia tri |
|-----------|---------|
| Method | `POST` |
| Route | `/api/auth/register` |
| Auth | Anonymous (`.AllowAnonymous()`) |
| Success Response | `201 Created` |
| Response Body | `UserDto` |
| Command | `RegisterUserCommand` |
| Handler | `RegisterUserCommandHandler` |

---

## Request Schema

```json
{
  "userName": "string",
  "email": "string",
  "password": "string",
  "currency": "string",
  "firstName": "string | null",
  "lastName": "string | null"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `UserName` | `string` | Yes | NotWhiteSpace, MinLength(3), MaxLength(50), Regex `^[a-zA-Z0-9_-]+$` |
| `Email` | `string` | Yes | NotWhiteSpace, MaxLength(255), Regex `^[^@\s]+@[^@\s]+\.[^@\s]+$` |
| `Password` | `string` | Yes | NotWhiteSpace, MinLength(8), MaxLength(128), Format: 1 uppercase + 1 lowercase + 1 digit + 1 special char |
| `Currency` | `string` | Yes (default `"VND"`) | InSet(`Currency.All`) - phai la currency hop le |
| `FirstName` | `string?` | No | Khi co gia tri: NotNullOrWhiteSpace, MinLength(1), MaxLength(50) |
| `LastName` | `string?` | No | Khi co gia tri: NotNullOrWhiteSpace, MinLength(1), MaxLength(50) |

**Password format rules** (tu `App.Constraint.Password`):
- It nhat 1 chu hoa
- It nhat 1 chu thuong
- It nhat 1 chu so
- It nhat 1 ky tu dac biet (non-alphanumeric)

---

## Response Schema (UserDto)

```json
{
  "id": "guid",
  "userName": "string",
  "email": "string",
  "emailConfirmed": false,
  "phoneNumber": null,
  "countryCode": null,
  "phoneNumberConfirmed": false,
  "twoFactorEnabled": false,
  "twoFactorProvider": "none",
  "status": "inactive",
  "createdAt": "datetime",
  "profile": {
    "firstName": "string | null",
    "lastName": "string | null",
    "displayName": "string | null",
    "fullName": "string | null",
    "avatarUrl": null,
    "dateOfBirth": null,
    "gender": null
  }
}
```

---

## Business Logic (numbered steps)

### Step 1: Input Validation
- `IHasValidate.Validate()` chay truoc handler (pipeline behavior)
- Kiem tra tat ca validation rules trong bang Request Schema o tren
- Tra ve `ViolationsError` neu bat ky field nao khong hop le

### Step 2: Create Value Objects
- `UserEmail.Create(request.Email)` → tao va normalize email
- `UserName.Create(request.UserName)` → tao va normalize username
- `Password.Create(request.Password, _passwordHasher)` → hash password
- `Currency.FromId(request.Currency)` → resolve currency enum
- `PersonName.Create(request.FirstName, request.LastName, request.UserName)` → tao ten hien thi

### Step 3: Duplicate Check
- Query DB: `AnyAsync(x => x.Email == email)` → neu ton tai: tra ve `User.Email.AlreadyExists` (409 Conflict)
- Query DB: `AnyAsync(x => x.UserName == userName)` → neu ton tai: tra ve `User.UserName.AlreadyExists` (409 Conflict)

### Step 4: User.Create()
Domain method `User.Create(userName, email, now, personName, currency, password)` thuc hien:
- Tao `UserId` moi (`Guid.CreateVersion7()`)
- Set initial state:
  - `EmailConfirmed = false`
  - `PhoneNumberConfirmed = false`
  - `TwoFactorEnabled = false`
  - `TwoFactorProvider = None`
  - **`Status = Inactive`**
  - `LockoutEnabled = true`
  - `AccessFailedCount = 0`
  - `Version = 0`
- **Init Profile**: `UserProfile(user.Id, now)` + `Profile.Update(now, personName)`
- **Init Wallet**: `Wallet.Create(user.Id, currency, now)`
- **Raise event**: `UserCreatedEvent(userId, userName, email, now)`

### Step 5: Assign Roles
- `user.AssignRole("bidder", now)` → tu `App.Roles.Definitions.Bidder.Name`
- `user.AssignRole("user", now)` → tu `App.Roles.Definitions.User.Name`

**Auto-assigned roles**: `bidder` (level 50) + `user` (level 50)

### Step 6: Persist
- `_dbContext.Insert(user)`
- `_unitOfWork.SaveChangesAsync()` → luu user + dispatch domain events

### Step 7: Domain Event - UserCreatedEvent
Handler `UserCreatedEventHandler` duoc trigger sau SaveChanges:
1. Log thong tin user moi
2. Tao email verification token: `_secureTokenStore.CreateTokenAsync(TokenType.EmailVerification, userId)`
3. Gui welcome email: `_mailNotifier.SendWelcomeVerifyAsync(email, userName, userId, token, expiry)`

---

## Error Codes

| HTTP Status | Error Code | Mo ta | Khi nao |
|------------|------------|-------|---------|
| 422 | `ViolationsError` | Validation that bai | Input khong hop le (email format, password yeu, username ngan, ...) |
| 409 | `User.Email.AlreadyExists` | Email da ton tai | `existByEmail == true` |
| 409 | `User.UserName.AlreadyExists` | Username da ton tai | `existByUserName == true` |
| 400 | `Currency.NotSupported` | Currency khong hop le | `Currency.FromId()` tra ve HasNoValue |

---

## Source Files

| Layer | File |
|-------|------|
| Command | `src/core/OIO.Application/Context/UserContext/Commands/RegisterUser/RegisterUserCommand.cs` |
| Handler | `src/core/OIO.Application/Context/UserContext/Commands/RegisterUser/RegisterUserCommandHandler.cs` |
| Domain | `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/User.cs` (method `Create`) |
| Event | `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/Events/UserCreatedEvent.cs` |
| Event Handler | `src/core/OIO.Application/Context/UserContext/EventHandlers/UserCreatedEventHandler.cs` |
| Endpoint | `src/presentation/OIO.Api/Endpoints/UserContext/Auth/RegisterUserEndpoint.cs` |
| Constraints | `src/core/OIO.Domain/AppDefinitions/AppConstraints.cs` |
| Roles | `src/core/OIO.Domain/AppDefinitions/AppRoles.cs` |
| DTO | `src/core/OIO.Application/Context/UserContext/DTOs/UserDto.cs` |
