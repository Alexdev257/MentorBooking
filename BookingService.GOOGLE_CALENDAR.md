# Phần Calendar đã xây & cách dùng API

## 1. Đã xây những gì

### 1.1 Tích hợp Google Calendar (Backend)

| Thành phần | Vị trí | Mô tả |
|------------|--------|--------|
| **IGoogleCalendarService** | `BookingService.Application/Interfaces/Services/IGoogleCalendarService.cs` | Interface: tạo event trên Google Calendar kèm link Google Meet. |
| **GoogleCalendarService** | `BookingService.Infrastructure/Services/GoogleCalendarService.cs` | Implementation: dùng **Service Account** (file JSON), gọi Google Calendar API để tạo event và request conference (hangoutsMeet) → trả về **EventId** và **Meet link**. |
| **Booking entity** | `BookingService.Domain/Entities/Booking.cs` | Thêm 2 field: `MeetingLink`, `GoogleEventId` (lưu link Meet và id event trên Google). |
| **BookingResponseDto** | `BookingService.Application/DTOs/Response/BookingResponseDto.cs` | Trả về thêm `MeetingLink`, `GoogleEventId` cho client. |
| **Accept flow** | `BookingService.Application/Services/BookingService.cs` | Khi mentor **Accept** booking: sau khi set status = Confirmed, gọi `CreateEventWithMeetAsync` → lưu event id và meet link vào booking. |

**Luồng tự động:**

1. Mentee tạo booking (POST `/api/booking/bookings`).
2. Mentor gọi **PATCH** `/api/booking/bookings/{bookingId}/accept` (kèm JWT).
3. Backend:
   - Cập nhật booking sang **Confirmed**.
   - Nếu bật Google Calendar: tạo event trên calendar (title = Topic, Start/End = ScheduleStart/ScheduleEnd), tạo Google Meet → gán `GoogleEventId` và `MeetingLink` vào booking.
   - Trả về booking (có `MeetingLink` trong response).

**Chưa xây:**

- API riêng “chỉ tạo event” (calendar chỉ được gọi nội bộ khi accept).
- Refresh token cho Auth.
- Xóa/cập nhật event trên Google khi booking bị cancel (có thể làm sau).

---

## 2. Cấu hình để dùng Google Calendar API

File: **BookingService.Api/appsettings.json** (hoặc appsettings.Development.json).

```json
"GoogleCalendar": {
  "Enabled": true,
  "CalendarId": "primary",
  "ServiceAccountJsonPath": "C:\\path\\to\\your-service-account.json",
  "TimeZone": "Asia/Ho_Chi_Minh"
}
```

| Key | Ý nghĩa |
|-----|---------|
| **Enabled** | `true` = bật tạo event + Meet khi accept; `false` = không gọi Google (chỉ cập nhật status). |
| **CalendarId** | Calendar tạo event (thường `"primary"` = calendar chính của service account). |
| **ServiceAccountJsonPath** | Đường dẫn tuyệt đối tới file JSON service account (tải từ Google Cloud Console). |
| **TimeZone** | Múi giờ cho event (optional). |

Nếu **Enabled = false** hoặc không có **ServiceAccountJsonPath** hợp lệ → Accept vẫn chạy, nhưng `MeetingLink` và `GoogleEventId` sẽ null.

---

## 3. Cách lấy Service Account (Google Cloud)

1. Vào [Google Cloud Console](https://console.cloud.google.com/) → chọn project (hoặc tạo mới).
2. **APIs & Services** → **Enable APIs** → bật **Google Calendar API**.
3. **APIs & Services** → **Credentials** → **Create credentials** → **Service account**.
4. Tạo service account → **Keys** → **Add key** → **JSON** → tải file.
5. Đặt file ở máy/server và cấu hình `ServiceAccountJsonPath` trỏ tới file đó.

**Lưu ý:** Tạo event có **Google Meet** thường cần calendar thuộc Google Workspace hoặc OAuth user; với service account đơn thuần có thể chỉ tạo được event (không có Meet). Khi đó `MeetingLink` có thể null nhưng event vẫn tạo được.

---

## 4. Cách sử dụng API (phía client)

**Không có API “calendar” riêng.** Calendar được gọi **nội bộ** khi mentor accept booking.

### Bước 1: Mentee tạo booking

```http
POST /api/booking/bookings
Authorization: Bearer <access_token_mentee>
Content-Type: application/json

{
  "mentorId": "<guid>",
  "slotId": "<guid>",
  "topic": "Ôn thi môn X",
  "notes": "Cần ôn chương 1–3",
  "priceAmount": 100000,
  "currency": "VND"
}
```

→ Booking tạo với status **Pending**.

### Bước 2: Mentor accept booking (kích hoạt tạo Calendar + Meet)

```http
PATCH /api/booking/bookings/{bookingId}/accept
Authorization: Bearer <access_token_mentor>
```

**Response 200** (khi đã bật Google Calendar và cấu hình đúng):

```json
{
  "isSuccess": true,
  "message": "Booking accepted. Meeting link has been created.",
  "data": {
    "id": "...",
    "mentorId": "...",
    "menteeId": "...",
    "slotId": "...",
    "status": 1,
    "topic": "Ôn thi môn X",
    "notes": "Cần ôn chương 1–3",
    "priceAmount": 100000,
    "currency": "VND",
    "scheduleStart": "2025-03-10T14:00:00Z",
    "scheduleEnd": "2025-03-10T15:00:00Z",
    "meetingLink": "https://meet.google.com/xxx-xxxx-xxx",
    "googleEventId": "abc123...",
    "createdAt": "..."
  }
}
```

- **meetingLink**: link Google Meet (nếu API trả về).
- **googleEventId**: id event trên Google Calendar (để sau này hủy/cập nhật nếu cần).

### Bước 3: Lấy thông tin booking (xem link Meet)

```http
GET /api/booking/bookings/{bookingId}
Authorization: Bearer <access_token>
```

Response có `meetingLink` và `googleEventId` như trên.

---

## 5. Tóm tắt

| Câu hỏi | Trả lời |
|---------|---------|
| Calendar đã xây gì? | Tích hợp Google Calendar: khi **Accept** booking thì tạo event + (nếu được) Google Meet, lưu `MeetingLink` và `GoogleEventId` vào booking. |
| API calendar gọi như thế nào? | **Không có endpoint riêng.** Client gọi **PATCH .../bookings/{id}/accept** (với JWT mentor) → backend tự gọi Google Calendar API và trả về booking kèm `meetingLink` trong response. |
| Cần cấu hình gì? | Trong appsettings: `GoogleCalendar:Enabled = true`, `ServiceAccountJsonPath` = đường dẫn file JSON service account, đã bật Google Calendar API trên GCP. |
