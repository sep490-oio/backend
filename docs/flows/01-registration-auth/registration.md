# Dang ky tai khoan (Registration)

## Tong quan
Nguoi dung tao tai khoan moi tren he thong OIO. Sau khi dang ky thanh cong, tai khoan duoc gan role `Bidder` va `User`. Email xac thuc se duoc gui rieng (xem [email-verification.md](./email-verification.md)).

## Actors
- **Nguoi dung chua dang ky** (Anonymous)

## Endpoint Sequence

### Step 1: Dang ky tai khoan

- **Method:** `POST /api/auth/register`
- **Auth:** Anonymous (`AllowAnonymous`)
- **Request:**
  ```json
  {
    "userName": "string (required, 3-50 ky tu, chi alphanumeric va dash)",
    "email": "string (required, email hop le)",
    "password": "string (required, 8-128 ky tu, theo quy tac mat khau)",
    "currency": "string (default: 'VND', phai nam trong danh sach ho tro)",
    "firstName": "string? (optional, 2-50 ky tu)",
    "lastName": "string? (optional, 2-50 ky tu)"
  }
  ```
- **Response:** `201 Created`
  ```json
  {
    "id": "guid",
    "userName": "string",
    "email": "string",
    "status": "string",
    "createdAt": "datetime"
  }
  ```
- **Loi co the xay ra:**
  - `422 Unprocessable Entity` - Validation loi (ten, email, password khong hop le)
  - `409 Conflict` - Email hoac UserName da ton tai

## Business Logic (Handler)

1. **Tao Value Objects:**
   - `UserEmail.Create(email)` - Normalize email
   - `UserName.Create(userName)` - Normalize username
   - `Password.Create(password, passwordHasher)` - Hash password
   - `Currency.FromId(currency)` - Validate currency

2. **Kiem tra trung lap:**
   - Tim user co cung `Email.Normalized` -> Tra ve `EmailAlreadyExists`
   - Tim user co cung `UserName.Normalized` -> Tra ve `UserNameAlreadyExists`

3. **Tao User aggregate:**
   - `PersonName.Create(firstName, lastName, userName)` - Ten hien thi
   - `User.Create(userName, email, nowUtc, personName, currency, password)`

4. **Gan role mac dinh:**
   - `user.AssignRole("Bidder", nowUtc)` - Cho phep dau gia
   - `user.AssignRole("User", nowUtc)` - Quyen co ban

5. **Luu va tra ket qua:** Insert user, SaveChanges, tra ve `UserDto`

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `UserCreatedEvent` | Phat ra khi user duoc tao thanh cong. Handler gui email xac thuc. |

## Luu y nghiep vu

- Password duoc hash truoc khi luu (IPasswordHasher)
- Email va UserName duoc normalize (lowercase, trim) de tranh trung lap
- Currency mac dinh la `VND`, phai thuoc danh sach `Currency.All`
- Tai khoan moi chua xac thuc email se khong the thuc hien mot so hanh dong (vd: tao item)
- Regex constraint cho UserName: chi cho phep `a-z`, `0-9`, `-`
- Regex constraint cho Email: theo chuan email thong thuong
