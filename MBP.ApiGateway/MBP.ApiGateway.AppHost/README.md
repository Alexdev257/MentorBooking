# MentorBooking AppHost (Aspire)

## Chạy AppHost

1. **Tắt mọi instance AppHost đang chạy** (Ctrl+C trong terminal đang chạy, hoặc đóng cửa sổ). Nếu không, khi build sẽ báo lỗi file bị khóa (process cannot access the file).

2. Chạy từ thư mục solution hoặc từ thư mục này:
   ```bash
   cd D:\FPT\SEM8\PRN232\MentorBooking\MBP.ApiGateway\MBP.ApiGateway.AppHost
   dotnet run
   ```
   Hoặc chỉ định profile:
   ```bash
   dotnet run --launch-profile https
   ```

3. Mở **Dashboard**: https://localhost:31900 (hoặc http://localhost:31901)

4. **PgAdmin**: http://localhost:15050 (Postgres: host `localhost`, port `15432`)

## Nếu gặp lỗi "address already in use"

- Đóng hết terminal/process đang chạy AppHost hoặc các service.
- Hoặc khởi động lại máy để giải phóng port.
- Chi tiết: xem `docs/Aspire_Troubleshooting.md`.

## Ports đang dùng (tránh conflict)

| Thành phần   | Port(s)        |
|-------------|-----------------|
| Dashboard   | 31900 (https), 31901 (http) |
| OTLP/Resource | 31801–31806   |
| Postgres    | 15432           |
| PgAdmin     | 15050           |
| Các API     | port động (0)   |
