# Lỗi Migration BookingService

## Lệnh chạy:
```powershell
dotnet ef migrations add InitBooking -s "../BookingService.Api/BookingService.Api.csproj"
```

## Lỗi xảy ra:
```
An error occurred while accessing the Microsoft.Extensions.Hosting services. 
Continuing without the application service provider. 
Error: Value cannot be null. (Parameter 's')

Unable to create a 'DbContext' of type ''. 
The exception 'No database provider has been configured for this DbContext. 
A provider can be configured by overriding the 'DbContext.OnConfiguring' method 
or by using 'AddDbContext' on the application service provider. 
If 'AddDbContext' is used, then also ensure that DbContext type accepts 
DbContextOptions<TContext> object in its constructor and passes it to the base constructor for DbContext.'
```

## Nguyên nhân:

1. **DbContext constructor không đúng**: 
   - File: [BookingService.Infrastructure/Persistence/BookingApplicationDbContext.cs](BookingService.Infrastructure/Persistence/BookingApplicationDbContext.cs)
   - `BookingApplicationDbContext` có constructor mặc định rỗng nhưng không override `OnConfiguring()` để setup DbContextOptions
   - Constructor constructor nhận `DbContextOptions<BookingApplicationDbContext>` nhưng lại gọi `OnConfiguring()` để thêm interceptor, điều này không hoạt động vì DbContextOptions đã được truyền vào từ constructor

2. **Lỗi trong OnConfiguring()**:
   - `_auditableEntityInterceptor` có thể là null khi `DbContext` được khởi tạo trong migration (vì constructor mặc định không khởi tạo nó)
   - Khi chạy migration, EF Core không thể resolve `AuditableEntityInterceptor` dependency

## Giải pháp:

1. Sửa constructor của `BookingApplicationDbContext`:
   ```csharp
   public BookingApplicationDbContext(DbContextOptions<BookingApplicationDbContext> options,
       AuditableEntityInterceptor auditableEntityInterceptor) : base(options)
   {
       _auditableEntityInterceptor = auditableEntityInterceptor;
   }
   ```

2. Sửa `OnConfiguring()` để kiểm tra `_auditableEntityInterceptor` trước khi thêm:
   ```csharp
   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
       if (_auditableEntityInterceptor != null)
       {
           optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
       }
   }
   ```

3. Hoặc tốt hơn: Cấu hình interceptor trong `Program.cs` thay vì trong `OnConfiguring()`:
   ```csharp
   services.AddDbContext<BookingApplicationDbContext>((sp, options) =>
   {
       options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
              .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
   });
   ```

## Lệnh chạy migration đúng:
```powershell
dotnet ef migrations add InitBooking -s BookingService.Api
```
(Không cần đặc tả đường dẫn đầy đủ nếu đã ở đúng thư mục)
