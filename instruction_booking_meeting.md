# Hướng dẫn và Luồng công việc: Booking Service & Meeting Service

Tài liệu này tổng kết nội dung và luồng hoạt động giữa `BookingService`, `MeetingService` và `MeetingRecordingBot` trong hệ thống MentorBooking.

## 1. Tổng quan các Service

### Booking Service
- **Chức năng**: Quản lý lịch trống của Mentor và các yêu cầu đặt lịch (Booking) từ Mentee.
- **Thành phần chính**:
    - `BookingService.cs`: Xử lý logic tạo slot, tạo booking, mentor chấp nhận/từ chối booking.
    - Kết hợp với `GoogleCalendarService` để tạo sự kiện và link Google Meet khi booking được xác nhận.
- **Sự kiện xuất bản**: `BookingAcceptedEvent` (Khi mentor bấm Accept).

### Meeting Service
- **Chức năng**: Quản lý các buổi họp (Meeting) thực tế dựa trên booking và điều phối việc quay video (Recording).
- **Thành phần chính**:
    - `BookingAcceptedConsumer`: Tạo bản ghi `Meeting` khi nhận được sự kiện từ `BookingService`.
    - `JoinMeetingCommandHandler`: Xử lý khi người dùng bắt đầu tham gia họp, đồng thời kích hoạt bot quay video.
    - `MeetingRecordingCompletedConsumer`: Lưu thông tin video sau khi bot hoàn thành việc quay.

### Meeting Recording Bot (Node.js)
- **Chức năng**: Tự động tham gia Google Meet bằng Puppeteer và quay phim màn hình bằng FFmpeg.
- **Thành phần chính**:
    - `index.js`: Lắng nghe `RecordMeetingCommand`, thực hiện quay, upload video lên Firebase Storage và gửi thông báo hoàn tất.

---

## 2. Luồng hoạt động chi tiết (End-to-End Flow)

### Giai đoạn 1: Đặt lịch và Xác nhận (Booking)
1. **Mentee** chọn slot và tạo yêu cầu đặt lịch thông qua `BookingService`. Trạng thái ban đầu là `Pending`.
2. **Mentor** xem danh sách booking và chọn **Accept**.
3. `BookingService` gọi Google API để tạo link **Google Meet**.
4. Trạng thái booking chuyển thành `Confirmed`.
5. Hệ thống gửi `BookingAcceptedEvent` tới Message Broker (RabbitMQ).

### Giai đoạn 2: Chuẩn bị Meeting
1. `MeetingService` nhận `BookingAcceptedEvent` thông qua `BookingAcceptedConsumer`.
2. Một bản ghi `Meeting` mới được tạo trong database của `MeetingService` với trạng thái `Pending` (0).

### Giai đoạn 3: Tham gia họp và Kích hoạt Bot
1. Khi đến giờ họp, **Mentor** nhấn nút "Join Meeting" trên giao diện.
2. Web UI gọi API `JoinMeetingCommand` tới `MeetingService`.
3. Nếu đây là lần đầu tiên có người tham gia (`Status == 0`):
    - Status của Meeting chuyển thành `On-going` (1).
    - Hệ thống bắt đầu một **10-minute delay** (để đợi mọi người ổn định và bắt đầu trao đổi).
    - Sau 10 phút, `MeetingService` gửi `RecordMeetingCommand` tới bot.

### Giai đoạn 4: Quay video (Recording)
1. **Recording Bot** nhận lệnh, khởi động trình duyệt Puppeteer và truy cập vào link Google Meet.
2. Bot tự động tắt Mic/Cam, điền tên và nhấn "Join".
3. **FFmpeg** được kích hoạt để bắt đầu quay màn hình desktop của bot.
4. Sau khi hết thời gian (duration), FFmpeg dừng quay và lưu thành file `.mp4`.

### Giai đoạn 5: Hoàn tất và Lưu trữ
1. Bot upload file video lên **Firebase Storage** và lấy public URL.
2. Bot gửi `MeetingRecordingCompletedEvent` về hệ thống.
3. `MeetingService` nhận sự kiện này:
    - Tạo bản ghi `MeetingRecording` với link video.
    - Chuyển trạng thái Meeting thành `Finished` (2).
4. Người dùng có thể xem lại video recording trong lịch sử cuộc họp.

---

## 3. Các điểm lưu ý quan trọng
- **Cơ chế Trigger Bot**: Bot không vào ngay lập tức mà đợi 10 phút sau khi Mentor bắt đầu vào link. Điều này giúp tiết kiệm tài nguyên và避免 quay những phút ban đầu chờ đợi.
- **Công nghệ sử dụng**: RabbitMQ (MassTransit) để giao tiếp async, Puppeteer + FFmpeg cho recording, Firebase Storage để lưu trữ video.
- **Status Meeting**: `0 (Pending)` -> `1 (On-going)` -> `2 (Finished)`.
