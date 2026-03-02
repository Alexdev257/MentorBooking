# Hướng dẫn test Booking & Google Calendar

## Chuẩn bị

1. **Chạy database**: PostgreSQL (theo ConnectionStrings trong appsettings).
2. **Migration**: Các project (Auth, Booking, Meeting, AI...) dùng EF Core **Migrations**. Khi thêm/sửa entity hoặc cấu hình, cần **add migration** trước, sau đó run project thì DB mới được cập nhật đúng.
   - Add migration: `dotnet ef migrations add <TênMigration> --project <Tên.Infrastructure> --startup-project <Tên.Api>`
   - Khi chạy project, `Program.cs` gọi `Migrate()` → áp dụng các migration còn pending vào DB.
   - Chi tiết: xem **docs/EF_CORE_MIGRATIONS_QUY_TRINH.md**.
3. **Chạy AuthService**: `dotnet run --project AuthService.Api` (mặc định http://localhost:5011).
3. **Chạy BookingService**: `dotnet run --project BookingService.Api` (mặc định http://localhost:5089).
4. **Có ít nhất 2 user trong DB**:
   - 1 **mentor** (teacher, Role = 2): dùng để tạo slot và accept booking.
   - 1 **mentee** (student, Role = 3): dùng để xem slot và tạo booking.

Nếu chưa có user, gọi API Admin (với JWT admin) để đăng ký teacher/student, hoặc seed DB.

**Lấy MentorId / MenteeId:** Sau khi login, copy `accessToken` và paste vào [jwt.io](https://jwt.io) (phần "Payload") → claim `sub` hoặc `UserId` chính là Id của user đó. Dùng Id này làm `mentorId` hoặc `menteeId` trong các request.

---

## Luồng test (thứ tự)

| Bước | Ai thực hiện | API | Mục đích |
|------|----------------|-----|----------|
| 1 | Mentor | Login | Lấy access token mentor |
| 2 | Mentor | Tạo slot | Tạo khung giờ trống để mentee book |
| 3 | Mentee | Login | Lấy access token mentee |
| 4 | Mentee | Xem slots | Lấy danh sách slot có sẵn của mentor |
| 5 | Mentee | Tạo booking | Đặt chỗ 1 slot → status Pending |
| 6 | Mentor | Accept booking | Chấp nhận → status Confirmed, **tạo event + Meet link** (nếu bật Calendar) |
| 7 | Bất kỳ | Get booking | Kiểm tra `meetingLink`, `googleEventId` trong response |

---

## Chi tiết từng bước

### Bước 1: Mentor đăng nhập

**Request:**
```http
POST http://localhost:5011/api/auth/login
Content-Type: application/json

{
  "email": "mentor@example.com",
  "password": "Mentor@123"
}
```

**Response (200):**
```json
{
  "isSuccess": true,
  "message": "Login successfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  }
}
```

→ **Lưu `data.accessToken`** để dùng cho các request của mentor (tạo slot, accept booking).

---

### Bước 2: Mentor tạo slot

Thay `{MENTOR_ID}` = Id của user mentor (có thể lấy từ JWT decode tại claim `sub` hoặc `UserId`).  
Thay `{ACCESS_TOKEN_MENTOR}` = token lấy ở bước 1.

**Request:**
```http
POST http://localhost:5089/api/booking/mentors/{MENTOR_ID}/slots
Authorization: Bearer {ACCESS_TOKEN_MENTOR}
Content-Type: application/json

{
  "startAt": "2025-03-15T14:00:00Z",
  "endAt": "2025-03-15T15:00:00Z"
}
```

**Response (200):** Slot vừa tạo (có `id`, `startAt`, `endAt`, `isBooked: false`).

→ **Lưu `data.id`** (slotId) và **mentorId** để dùng khi tạo booking.

---

### Bước 3: Mentee đăng nhập

```http
POST http://localhost:5011/api/auth/login
Content-Type: application/json

{
  "email": "mentee@example.com",
  "password": "Mentee@123"
}
```

→ **Lưu `data.accessToken`** cho mentee.

---

### Bước 4: Mentee xem slots có sẵn

```http
GET http://localhost:5089/api/booking/mentors/{MENTOR_ID}/slots?from=2025-03-01&to=2025-03-31
Authorization: Bearer {ACCESS_TOKEN_MENTEE}
```

→ Kiểm tra có slot vừa tạo (chưa bị book). Lấy `slotId` nếu chưa có.

---

### Bước 5: Mentee tạo booking

**Request:**
```http
POST http://localhost:5089/api/booking/bookings
Authorization: Bearer {ACCESS_TOKEN_MENTEE}
Content-Type: application/json

{
  "mentorId": "{MENTOR_ID}",
  "slotId": "{SLOT_ID}",
  "topic": "Ôn thi môn Toán",
  "notes": "Cần ôn chương 1–3",
  "priceAmount": 100000,
  "currency": "VND"
}
```

**Response (201):** Booking với `status: 0` (Pending). Lưu `data.id` (bookingId).

---

### Bước 6: Mentor accept booking (kích hoạt Calendar)

**Request:**
```http
PATCH http://localhost:5089/api/booking/bookings/{BOOKING_ID}/accept
Authorization: Bearer {ACCESS_TOKEN_MENTOR}
```

**Response (200):**
- Nếu **đã bật Google Calendar** và cấu hình đúng: `data.meetingLink` và `data.googleEventId` có giá trị.
- Nếu chưa bật / lỗi: `meetingLink` và `googleEventId` có thể null, booking vẫn Confirmed.

---

### Bước 7: Kiểm tra booking (xem link Meet)

```http
GET http://localhost:5089/api/booking/bookings/{BOOKING_ID}
Authorization: Bearer {ACCESS_TOKEN_MENTEE}
```
(hoặc dùng token mentor)

→ Response có `meetingLink`, `googleEventId` nếu đã tạo event thành công.

---

## Test nhanh bằng file .http (VS Code / Rider)

Dùng file **BookingService.Api/BookingService.Api.http** (đã thêm các request mẫu):

1. Chạy **Bước 1** (Login mentor) → copy `accessToken` từ response vào biến `@mentorToken`.
2. Chạy **Bước 2** (Create slot) → copy `data.id` vào `@slotId`, đảm bảo `@mentorId` đúng.
3. Chạy **Bước 3** (Login mentee) → copy `accessToken` vào `@menteeToken`.
4. Chạy **Bước 5** (Create booking) → copy `data.id` vào `@bookingId`.
5. Chạy **Bước 6** (Accept) → xem response có `meetingLink` không.
6. Chạy **Bước 7** (Get booking) để xác nhận lại.

---

## Bật Google Calendar khi test

Trong **BookingService.Api/appsettings.Development.json** (hoặc appsettings.json):

```json
"GoogleCalendar": {
  "Enabled": true,
  "CalendarId": "primary",
  "ServiceAccountJsonPath": "C:\\path\\to\\service-account.json",
  "TimeZone": "Asia/Ho_Chi_Minh"
}
```

- Đặt **Enabled = true**.
- **ServiceAccountJsonPath**: đường dẫn file JSON service account (đã bật Google Calendar API trên GCP).
- Restart BookingService sau khi sửa config.

Nếu **Enabled = false** hoặc không có file JSON: Accept vẫn thành công, nhưng `meetingLink` và `googleEventId` sẽ null.

---

## Lỗi thường gặp

| Hiện tượng | Nguyên nhân / Cách xử lý |
|------------|---------------------------|
| 401 Unauthorized | Token hết hạn hoặc sai → login lại lấy token mới. |
| 403 Forbidden (mentee/mentor bookings) | Đang dùng token user A nhưng gửi menteeId/mentorId của user B → dùng đúng token (mentor vs mentee). |
| Slot is already booked | Slot đã được book → tạo slot mới hoặc dùng slot khác. |
| Booking not found | Sai bookingId hoặc booking thuộc user khác. |
| meetingLink = null sau Accept | Chưa bật Google Calendar, hoặc sai ServiceAccountJsonPath, hoặc lỗi quyền GCP (Calendar API / Meet). Xem log BookingService. |
