# Luồng Booking Service & Meeting Service

## Tổng quan

- **BookingService**: slot, booking, Zoom meeting, **webhook Zoom** (`POST /api/zoom/webhooks`).
- **MeetingService**: tạo bản ghi `Meeting` khi mentor accept (`BookingAcceptedEvent`), cập nhật trạng thái qua **`ZoomMeetingLifecycleEvent`** (từ webhook Zoom).

Recording cloud của Zoom vẫn được xử lý trong webhook `recording.completed` (ghi link vào `booking.Notes`).

**Không còn** endpoint join trong MeetingService, **không còn** bot Puppeteer/FFmpeg (`MeetingRecordingBot` đã gỡ).

---

## Trạng thái Meeting (MeetingService DB)

| Status | Ý nghĩa |
|--------|--------|
| 0 | Pending (vừa tạo khi accept booking) |
| 1 | On-going (Zoom báo meeting started hoặc participant joined) |
| 2 | Finished (Zoom báo meeting ended) |

---

## Luồng chính

1. Mentee tạo booking → mentor **Accept** → Zoom tạo meeting → publish `BookingAcceptedEvent` → MeetingService tạo `Meeting` (Status = 0).
2. Người dùng join qua **link trong email** (không qua API join).
3. Zoom gửi webhook tới `POST /api/zoom/webhooks`:
   - `meeting.started` hoặc `meeting.participant_joined` → BookingService publish `ZoomMeetingLifecycleEvent(BookingId, 1)` → MeetingService set `Meeting.Status = 1` (nếu đang 0).
   - `meeting.ended` → cập nhật booking Completed + publish `ZoomMeetingLifecycleEvent(BookingId, 2)` → MeetingService set `Meeting.Status = 2`.

**Yêu cầu:** Cấu hình Zoom App để subscribe các event trên; URL webhook trỏ tới API BookingService (public HTTPS). Cần **RabbitMQ** (và `RabbitMQ:Host` trong config) để event tới MeetingService — giống luồng `BookingAcceptedEvent`.

---

## Ghi chú

- `Booking.GoogleEventId` lưu **Zoom meeting id** (chuỗi) để match webhook.
- Nếu không bật RabbitMQ, `IMessageProducer` là no-op → Meeting không đổi status từ webhook (booking vẫn cập nhật khi `meeting.ended` trong cùng process).
