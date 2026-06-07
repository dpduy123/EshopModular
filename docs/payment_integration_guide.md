# Hướng dẫn Tích hợp và Triển khai Module Payment

Tài liệu này hướng dẫn chi tiết cách triển khai, cấu hình và tích hợp module **Payment** mới vào hệ thống Eshop Modular Monolith.

---

## 1. Cấu trúc thư mục của Module Payment

Module Payment được thiết kế độc lập dưới dạng một module con trong kiến trúc Monolith. Cấu trúc thư mục của module nằm tại [src/Modules/Payment/Payment/](file:///Users/nguyenloan/Desktop/SE361/EshopModularMonoliths-main/src/Modules/Payment/Payment/):

```text
src/Modules/Payment/Payment/
├── Data/
│   └── PaymentDbContext.cs       # Quản lý kết nối DB và cấu hình schema riêng
├── Migrations/                   # Lưu trữ các file migration của EF Core
│   ├── <Timestamp>_InitialCreate.cs
│   └── PaymentDbContextModelSnapshot.cs
├── GlobalUsings.cs               # Khai báo using dùng chung trong module
├── Payment.csproj                # File cấu hình project của module
└── PaymentModule.cs              # Định nghĩa các dịch vụ DI và Pipeline của module
```

---

## 2. Chi tiết Mã nguồn đã triển khai

### A. Cấu hình Project (`Payment.csproj`)
Dự án Payment cần tham chiếu đến các thư viện dùng chung trong thư mục `Shared`.
File [Payment.csproj](file:///Users/nguyenloan/Desktop/SE361/EshopModularMonoliths-main/src/Modules/Payment/Payment/Payment.csproj) chứa định nghĩa:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Shared.Messaging\Shared.Messaging.csproj" />
    <ProjectReference Include="..\..\..\Shared\Shared\Shared.csproj" />
  </ItemGroup>
</Project>
```

### B. Lớp Context Cơ sở dữ liệu (`PaymentDbContext.cs`)
File [PaymentDbContext.cs](file:///Users/nguyenloan/Desktop/SE361/EshopModularMonoliths-main/src/Modules/Payment/Payment/Data/PaymentDbContext.cs) chịu trách nhiệm kết nối database và phân vùng dữ liệu bằng schema riêng biệt (`payment`):
```csharp
namespace Payment.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Thiết lập schema riêng cho module Payment để tránh xung đột bảng với Catalog, Basket...
        builder.HasDefaultSchema("payment");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());        
        base.OnModelCreating(builder);
    }
}
```

### C. Lớp Entry Point của Module (`PaymentModule.cs`)
File [PaymentModule.cs](file:///Users/nguyenloan/Desktop/SE361/EshopModularMonoliths-main/src/Modules/Payment/Payment/PaymentModule.cs) định nghĩa các Service Registration và Middleware Pipeline cho module:
```csharp
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Data.Interceptors;

namespace Payment;

public static class PaymentModule
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        // Đăng ký các Interceptor phục vụ cho ghi log lịch sử và dispatch sự kiện
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        // Cấu hình Database Context sử dụng PostgreSQL (Npgsql)
        services.AddDbContext<PaymentDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    public static IApplicationBuilder UsePaymentModule(this IApplicationBuilder app)
    {
        // Tự động chạy migration khi khởi động ứng dụng
        app.UseMigration<PaymentDbContext>();

        return app;
    }
}
```

---

## 3. Tích hợp Module Payment vào ứng dụng Bootstrapper API

Để ứng dụng chính nhận diện và chạy được module Payment, chúng ta thực hiện tích hợp trong project `Api`.

### A. Đăng ký tham chiếu dự án (`Api.csproj`)
File [Api.csproj](file:///Users/nguyenloan/Desktop/SE361/EshopModularMonoliths-main/src/Bootstrapper/Api/Api.csproj) đã thêm tham chiếu đến project Payment:
```xml
<ProjectReference Include="..\..\Modules\Payment\Payment\Payment.csproj" />
```

### B. Khai báo Global Usings (`GlobalUsings.cs`)
File [GlobalUsings.cs](file:///Users/nguyenloan/Desktop/SE361/EshopModularMonoliths-main/src/Bootstrapper/Api/GlobalUsings.cs) của dự án Api đã khai báo:
```csharp
global using Payment;
```

### C. Đăng ký trong `Program.cs`
Trong file [Program.cs](file:///Users/nguyenloan/Desktop/SE361/EshopModularMonoliths-main/src/Bootstrapper/Api/Program.cs), module Payment được tích hợp đầy đủ như sau:

1. **Quét Assembly để tích hợp Carter, MediatR và MassTransit**:
   ```csharp
   var paymentAssembly = typeof(PaymentModule).Assembly;

   builder.Services
       .AddCarterWithAssemblies(..., paymentAssembly);

   builder.Services
       .AddMediatRWithAssemblies(..., paymentAssembly);

   builder.Services
       .AddMassTransitWithAssemblies(builder.Configuration, ..., paymentAssembly);
   ```

2. **Đăng ký Services của Module**:
   ```csharp
   builder.Services
       .AddCatalogModule(builder.Configuration)
       .AddBasketModule(builder.Configuration)
       .AddOrderingModule(builder.Configuration)
       .AddPaymentModule(builder.Configuration); // Đăng ký Payment Module
   ```

3. **Kích hoạt Middleware (Migration tự động)**:
   ```csharp
   app
       .UseCatalogModule()
       .UseBasketModule()
       .UseOrderingModule()
       .UsePaymentModule(); // Kích hoạt Payment Module
   ```

---

## 4. Hướng dẫn Migration và Khởi chạy Hệ thống

### Bước 1: Tạo file Migration mới cho Payment
Nếu bạn thay đổi cấu hình thực thể (Entities) của Payment sau này và cần cập nhật DB:
1. Mở terminal tại thư mục gốc của giải pháp (`/Users/nguyenloan/Desktop/SE361/EshopModularMonoliths-main`).
2. Nếu máy bạn chưa cài đặt công cụ `dotnet-ef`, chạy lệnh sau để cài đặt:
   ```bash
   dotnet tool install --global dotnet-ef
   ```
3. Đảm bảo thư mục cài đặt công cụ `.dotnet/tools` đã được add vào PATH của hệ điều hành. Nếu sử dụng `zsh`, chạy:
   ```bash
   export PATH="$PATH:$HOME/.dotnet/tools"
   ```
4. Thực hiện tạo migration:
   ```bash
   dotnet ef migrations add <TênMigration> \
     --project src/Modules/Payment/Payment \
     --startup-project src/Bootstrapper/Api \
     --context PaymentDbContext
   ```

### Bước 2: Chạy và áp dụng thay đổi vào Server
Kiến trúc đã được thiết lập cơ chế **Auto-Migration** khi ứng dụng chạy. Bạn chỉ cần build lại và khởi động ứng dụng:

* **Đối với Docker Compose**:
  Chạy lệnh build lại image và chạy container:
  ```bash
  docker compose down
  docker compose up --build -d
  ```

* **Đối với dotnet CLI (chạy trực tiếp)**:
  ```bash
  dotnet run --project src/Bootstrapper/Api
  ```

Khi Server khởi chạy, nó sẽ tự động chạy các migration chưa được áp dụng và sinh ra schema `"payment"` cùng các bảng cơ sở dữ liệu tương ứng.
