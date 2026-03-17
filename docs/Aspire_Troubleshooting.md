# Aspire Troubleshooting – MentorBooking

## 1. Build báo "The process cannot access the file ... MBP.ApiGateway.AppHost.exe"

**Nguyên nhân:** AppHost đang chạy (process cũ) nên file .exe bị khóa.

**Cách xử lý:**
- Tắt hết instance AppHost: Ctrl+C trong terminal đang chạy AppHost, hoặc đóng cửa sổ terminal đó.
- Trong Task Manager có thể tìm process `MBP.ApiGateway.AppHost` hoặc `dotnet` và tắt.
- Sau đó chạy lại `dotnet build` hoặc `dotnet run`.

## 2. Lỗi "address already in use" / "Only one usage of each socket address"

**Nguyên nhân:** Port đã bị process khác (AppHost cũ, Postgres, PgAdmin, service khác) chiếm.

**Đã cấu hình trong solution:**
- AppHost Dashboard: `https://localhost:31900`, `http://localhost:31901`
- OTLP/Resource: port 31801–31806
- Postgres: host port **15432** (tránh trùng Postgres local 5432)
- PgAdmin: **15050** (tránh trùng 5050)
- Các API: port **0** (động) khi chạy qua AppHost

**Cách xử lý khi vẫn báo "address already in use":**
1. Tắt hết instance AppHost và các service đang chạy.
2. Nếu đang chạy Postgres/PgAdmin ngoài AppHost: tắt chúng hoặc dùng port khác.
3. Chỉ chạy **một** lần AppHost; mỗi lần test mới: stop AppHost cũ rồi mới `dotnet run` lại.

## 3. Lỗi "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL ... not set"

Đảm bảo trong `MBP.ApiGateway.AppHost/Properties/launchSettings.json` (profile `https`) có đủ:
- `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL`
- `DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL`
- `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL`

Các giá trị đang dùng port 31xxx để tránh trùng.

## 4. Chạy AppHost đúng cách

1. Mở terminal tại solution hoặc tại `MBP.ApiGateway.AppHost`.
2. Chạy: `dotnet run` (sẽ dùng profile `https` mặc định).
3. Mở trình duyệt: **https://localhost:31900** để vào Dashboard.

Chi tiết thêm: `MBP.ApiGateway/MBP.ApiGateway.AppHost/README.md`.
