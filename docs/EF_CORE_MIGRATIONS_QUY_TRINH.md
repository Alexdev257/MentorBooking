# Quy trình EF Core Migrations trong solution

## Quy tắc chung

**Có.** Với các project dùng EF Core **Migrations**, bạn cần **add migration** trước. Khi **run project**, ứng dụng sẽ áp dụng các migration còn **pending** vào database. Nếu chưa add migration thì không có bản ghi migration nào để áp dụng → database không được cập nhật theo model mới.

---

## Luồng đúng

| Bước | Việc cần làm |
|------|----------------------|
| 1 | Sửa **entity** hoặc **configuration** (DbContext, Fluent API) trong project Domain/Infrastructure. |
| 2 | **Add migration** để tạo file migration (code C# mô tả thay đổi schema). |
| 3 | **Run project** (hoặc chạy `dotnet ef database update`). Ứng dụng gọi `Migrate()` → EF so sánh `__EFMigrationsHistory` với danh sách migration → chạy các migration **chưa áp dụng** (pending) → cập nhật DB. |

Nếu bỏ bước 2 (không add migration) thì bước 3 không có gì để áp dụng → DB không thay đổi.

---

## Lệnh add migration theo từng service

Thư mục gốc: `D:\FPT\SEM8\PRN232\MentorBooking` (hoặc mở terminal tại solution root).

| Service | Add migration |
|---------|-------------------------------|
| **AuthService** | `dotnet ef migrations add <TênMigration> --project AuthService.Infrastructure --startup-project AuthService.Api` |
| **BookingService** | `dotnet ef migrations add <TênMigration> --project BookingService.Infrastructure --startup-project BookingService.Api` |
| **MeetingService** | `dotnet ef migrations add <TênMigration> --project MeetingService.Infrastructure --startup-project MeetingService.Api` |
| **AIService** | `dotnet ef migrations add <TênMigration> --project AIService.Infrastructure --startup-project AIService.Api` |

Ví dụ:

```bash
# Auth: thêm migration mới tên "AddUserPhone"
dotnet ef migrations add AddUserPhone --project AuthService.Infrastructure --startup-project AuthService.Api

# Booking: thêm cột mới cho bảng bookings
dotnet ef migrations add AddBookingReminder --project BookingService.Infrastructure --startup-project BookingService.Api
```

Sau khi add xong, **chạy project** (hoặc `dotnet ef database update --project ... --startup-project ...`) để cập nhật DB.

---

## Cách từng project áp dụng migration khi chạy

Các project **Auth**, **Booking**, **Meeting**, **AIService** đều có trong `Program.cs`:

- Gọi `db.Database.GetPendingMigrations()`.
- Nếu có migration pending → gọi `db.Database.Migrate()`.

Nghĩa là: khi bạn **đã add migration** và **run project**, migration mới sẽ tự được áp dụng vào DB (nếu kết nối DB đúng và có quyền ghi).

---

## Tóm tắt

- **Chưa add migration** → run project **không** cập nhật schema DB theo model mới.
- **Đã add migration** → run project (hoặc `database update`) → DB được cập nhật đúng theo migration.

Vì vậy: **bắt buộc add migration trước**, rồi mới run project thì database mới cập nhật đúng.
