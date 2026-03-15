# Hướng dẫn triển khai các tính năng Zoom nâng cao

Tài liệu này chi tiết các bước cần thực hiện để nâng cấp hệ thống MentorBooking với các tính năng: Group Mentoring, Cloud Recording, Webhooks và Attendance Reports.

---

## 1. Group Mentoring (Nhiều Mentee tham gia)
**Mục tiêu:** Cho phép 1 chủ phòng (Primary Mentee) mời thêm nhiều khách vào cùng buổi họp.

### Bước 1: Database & Domain
- **Tạo Entity `BookingParticipant`**: `Id`, `BookingId`, `MenteeId` (nullable cho khách vãng lai), `Email`, `Name`, `ZoomJoinUrl`.
- **Cập nhật `Booking`**: Thêm quan hệ `1-n` với `BookingParticipant`.
- **Cập nhật `BookingApplicationDbContext`**: Đăng ký Entity mới và chạy Migration.

### Bước 2: Zoom Service Integration
- **Hàm `AddRegistrantAsync`**:
    - Endpoint: `POST /meetings/{meetingId}/registrants`
    - Mục đích: Đăng ký từng email vào Zoom để nhận về `join_url` cá nhân hóa.

---

## 2. Cloud Recording (Tự động ghi hình)
**Mục tiêu:** Sử dụng hạ tầng của Zoom để quay video thay vì dùng Bot riêng.

### Bước 3: Cập nhật Lệnh tạo Meeting
- Trong `CreateMeetingAsync` của `ZoomService`, cập nhật Body request:
    ```json
    "settings": {
        "auto_recording": "cloud",
        "recording_encryption": true
    }
    ```

### Bước 4: Lấy link Recording
- **Cách 1 (Pull):** Gọi API `GET /meetings/{meetingId}/recordings` sau khi họp xong.
- **Cách 2 (Push - Khuyên dùng):** Cấu hình Webhook để nhận sự kiện `recording.completed`.

---

## 3. Webhooks (Đồng bộ trạng thái thực tế)
**Mục tiêu:** Hệ thống tự động biết khi nào buổi dạy bắt đầu/kết thúc mà không cần Mentor thao tác trên Web.

### Bước 5: Tạo Webhook Controller
- Tạo endpoint `POST /api/zoom/webhooks` (Yêu cầu `AllowAnonymous`).
- Xử lý các Event:
    - `meeting.started`: Chuyển trạng thái Booking/Meeting sang `On-going`.
    - `meeting.ended`: Chuyển trạng thái sang `Completed`.
    - `recording.completed`: Lấy `download_url` của video và lưu vào bảng `MeetingRecording`.

### Bước 6: Cấu hình trên Zoom App Marketplace
- Kích hoạt tính năng **Event Subscriptions**.
- Điền URL của Controller bạn vừa tạo vào phần **Event Notification Endpoint URL**.

---

## 4. Attendance Reports (Điểm danh tự động)
**Mục tiêu:** Đo lường mức độ chuyên cần của Mentee.

### Bước 7: Lấy dữ liệu điểm danh
- Sau khi nhận được sự kiện `meeting.ended`, gọi API:
    - Endpoint: `GET /report/meetings/{meetingId}/participants`
- **Logic lưu trữ:**
    - Tính toán: `TotalMinutes = (JoinTime - LeaveTime)`.
    - Cập nhật vào bảng `BookingParticipant`: Lưu số phút thực tế tham gia của từng người.

---

## TỔNG KẾT LƯU ĐỒ CÔNG VIỆC MỚI

1. **Booking**: Mentee tạo đặt chỗ + Nhập danh sách email tham gia.
2. **Accept**:
    - Mentor nhấn Accept.
    - Hệ thống tạo Zoom Meeting (Bật Cloud Recording).
    - Hệ thống đăng ký Registrant cho tất cả email -> Lưu Link riêng vào `BookingParticipant`.
3. **Meeting**:
    - Mentor/Mentee click vào Link riêng của mình.
    - **Webhook Zoom** báo về máy chủ -> Website hiển thị trạng thái "Đang dạy".
4. **End**:
    - Cuộc họp kết thúc.
    - **Webhook Zoom** báo về -> Hệ thống tự động điểm danh + Lấy bản ghi video.
    - Website cập nhật trạng thái "Hoàn thành" và hiển thị Video xem lại ngay lập tức.
    
---

## TỔNG KẾT CÁC TÍNH NĂNG ĐÃ TRIỂN KHAI

Hệ thống hiện tại đã tích hợp đầy đủ các thành phần sau:

1.  **Group Mentoring**: Tự động tạo `BookingParticipant` cho từng Mentee và khách mời. Đăng ký từng người với Zoom để lấy **Join URL cá nhân hóa** (Tránh việc chung link gây lộn xộn).
2.  **Email Invitations**: 
    - Gửi Mail kèm Join Link riêng cho từng Mentee.
    - Gửi Mail kèm **Host Link (Start URL)** riêng cho Mentor để có quyền quản trị phòng họp.
3.  **Cloud Recording**: Cấu hình tự động ghi âm trên đám mây ngay khi cuộc họp bắt đầu, kèm mã hóa bảo mật.
4.  **Zoom Webhooks (Real-time)**: 
    - Endpoint: `/api/zoom/webhooks`.
    - Hỗ trợ **CRC Verification** (Xác thực bảo mật từ Zoom).
    - Tự động nhận biết khi nào Mentor mở phòng (`meeting.started`) và đóng phòng (`meeting.ended`).
5.  **Attendance & Recording Automation**:
    - Tự động lấy **Attendance Report** (Số phút tham gia của từng người) ngay khi kết thúc.
    - Tự động lấy **Playback Link** của video ghi âm (`recording.completed`) để cập nhật vào thông tin buổi học.

---

## ĐIỀU KIỆN ĐỂ VẬN HÀNH THÀNH CÔNG

Để các tính năng trên hoạt động, bạn cần đảm bảo các điều kiện sau:

### 1. Cấu hình Zoom App (Marketplace)
- **App Type**: Phải là **Server-to-Server OAuth**.
- **Scopes (Quyền hạn)**: Phải tick đủ các scope sau:
    - `meeting:write:admin`, `meeting:read:admin`
    - `recording:read:admin`
    - `report:read:admin`
    - `user:read:admin`
- **Event Subscriptions (Webhooks)**: Bật tính năng này và đăng ký các event:
    - Meeting: `Start Meeting`, `End Meeting`.
    - Recording: `All recordings have completed`.

### 2. Cấu hình Code (`appsettings.json`)
Cần điền đầy đủ 4 thông số trong mục `"Zoom"`:
- `AccountId`, `ClientId`, `ClientSecret`.
- **`SecretToken`**: Lấy từ tab *Feature -> Event Subscriptions* trên Zoom Marketplace (Dùng để xác thực Webhook).

### 3. Môi trường triển khai
- Endpoint Webhook (`/api/zoom/webhooks`) phải **công khai** (có thể truy cập từ internet). 
- Nếu đang chạy dưới localhost để test, bạn bắt buộc phải dùng công cụ như **ngrok** để chuyển tiếp dữ liệu từ Zoom về máy mình.
