# Sửa lỗi: relation "users" does not exist (AuthService)

## Nguyên nhân

- EF Core báo **Pending migrations: 0** (bảng `__EFMigrationsHistory` đã có bản ghi).
- Nhưng bảng **users** (hoặc **Users**) không tồn tại trong database (DB mới, hoặc bảng bị xóa).
- Cấu hình entity đã được chỉnh lại dùng bảng **Users** (trùng với migration thứ hai).

## Cách xử lý

### Cách 1: Xóa lịch sử migration rồi khởi động lại (khuyến nghị cho dev)

Kết nối vào PostgreSQL (database `auth_db`), chạy:

```sql
DELETE FROM "__EFMigrationsHistory";
```

Sau đó **restart AuthService**. Ứng dụng sẽ thấy còn migration chưa chạy, tự chạy `Migrate()` và tạo đủ bảng (users, Teachers, Students, ...).

### Cách 2: Chạy migration từ command line

Đảm bảo database đã tồn tại, rồi chạy:

```bash
cd D:\FPT\SEM8\PRN232\MentorBooking
dotnet ef database update --project AuthService.Infrastructure --startup-project AuthService.Api
```

Nếu vẫn báo "No pending migrations" nhưng bảng không có, dùng Cách 1 (xóa `__EFMigrationsHistory`) rồi chạy lại lệnh trên.

### Cách 3: Tạo lại database (chỉ dùng khi không cần giữ dữ liệu)

1. Xóa database `auth_db` (hoặc tạo database mới với tên khác và đổi connection string).
2. Khởi động lại AuthService; migration sẽ chạy trên database trống và tạo đủ bảng.

## Đã sửa trong code

- **UserConfiguration**: `ToTable("users")` → `ToTable("Users")` để khớp với migration thứ hai (bảng được đổi tên thành **Users**).
- **Program.cs**: Bắt lỗi PostgreSQL `42P01` (relation does not exist) khi seed admin và ghi log hướng dẫn xóa `__EFMigrationsHistory` rồi restart.

Sau khi làm một trong các cách trên, chạy lại AuthService và kiểm tra seed admin (mặc định `admin@localhost` / `Admin@123` nếu có cấu hình).
