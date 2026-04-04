---
name: api-dev
description: >
  Develop a new API feature in the MentorBooking microservices project.
  Triggered when user says: /api-dev, "tạo api", "thêm api", "build api",
  "create endpoint", "add feature [service]", "implement api for".
  Handles all 4 layers: Domain entity, Application (DTO + Service + Mapper),
  Infrastructure (UnitOfWork registration), API Controller.
---

You are building a new API feature inside the **MentorBooking** microservices project.
Follow every rule below **strictly**. Do not skip steps or add unrequested extras.

---

## 0. CLARIFY BEFORE CODING

Before writing any code, ask the user:
1. **Which service?** (AuthService, BookingService, AIService, etc.)
2. **What entity/feature?** (e.g., Review, Payment, Notification)
3. **What endpoints are needed?** (list HTTP verbs + routes)
4. **Any special business rules?** (roles, ownership checks, validations)

Do NOT write code until you have answers.

---

## 1. PROJECT ARCHITECTURE RULES

Each microservice has exactly **4 layers**. All new code must go in the correct layer:

```
{Service}.Api/               ← Controllers only
{Service}.Application/       ← DTOs, Interfaces, Services, Mapping
{Service}.Domain/            ← Entities, Enums
{Service}.Infrastructure/    ← DbContext config, Repositories (UnitOfWork), DI registration
```

Shared packages (read-only, never modify):
- `Shared.Kernel` – Base entities (`BaseEntity`, `AuditableEntity`)
- `Shared.Contracts` – `CommonResponse<T>`, `Errors`
- `Shared.Infrastructure` – `IGenericRepository<T>`, `GenericRepository<T>`, middleware

---

## 2. DOMAIN LAYER

### Entity rules
- Inherit from `AuditableEntity` (gives `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `IsDeleted`, `DeletedAt`)
- `Id` is `Guid`, never int/long
- Use `int` for status/role fields backed by an enum
- Navigation properties are `virtual`
- No business logic in entities – entities are pure data

```csharp
// {Service}.Domain/Entities/{Entity}.cs
public class Review : AuditableEntity
{
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
    public int Rating { get; set; }          // 1-5
    public string? Comment { get; set; }
    public int Status { get; set; } = (int)ReviewStatusEnum.Active;

    public virtual Teacher? Teacher { get; set; }
    public virtual Student? Student { get; set; }
}
```

### Enum rules
- File: `{Service}.Domain/Enum/{Entity}{Type}Enum.cs`
- Values start at 0 for default state

```csharp
public enum ReviewStatusEnum
{
    Active = 0,
    Deleted = 1
}
```

---

## 3. APPLICATION LAYER

### 3a. DTOs

**Request DTOs** → `{Service}.Application/DTOs/Request/{Feature}/`
- Class name: `{Action}{Feature}Request` (e.g., `CreateReviewRequest`, `UpdateSlotRequest`)
- Use Data Annotations for validation: `[Required]`, `[MaxLength]`, `[Range]`, `[EmailAddress]`
- Keep only fields the client should send (no `Id`, `CreatedAt`, etc.)

```csharp
public class CreateReviewRequest
{
    [Required] public Guid TeacherId { get; set; }
    [Required][Range(1, 5)] public int Rating { get; set; }
    [MaxLength(1000)] public string? Comment { get; set; }
}
```

**Response DTOs** → `{Service}.Application/DTOs/Response/{Feature}/`
- Class name: `{Feature}ResponseDto` (e.g., `ReviewResponseDto`)
- Include all fields the client needs, flatten navigation where useful

```csharp
public class ReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Paged Request** (when listing with pagination):
```csharp
public class GetReviewsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? TeacherId { get; set; }
}
```

### 3b. Service Interface → `{Service}.Application/Interfaces/Services/I{Feature}Service.cs`

```csharp
public interface IReviewService
{
    Task<CommonResponse<ReviewResponseDto>> CreateReviewAsync(Guid studentId, CreateReviewRequest request, CancellationToken cancellationToken = default);
    Task<CommonResponse<ReviewResponseDto>> GetReviewByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CommonResponse<IEnumerable<ReviewResponseDto>>> GetReviewsByTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task<CommonResponse<ReviewResponseDto>> UpdateReviewAsync(Guid studentId, Guid reviewId, UpdateReviewRequest request, CancellationToken cancellationToken = default);
    Task<CommonResponse<bool>> DeleteReviewAsync(Guid studentId, Guid reviewId, CancellationToken cancellationToken = default);
}
```

### 3c. UnitOfWork Interface → `{Service}.Application/Interfaces/Repositories/I{Service}UnitOfWork.cs`

Add the new repository to the **existing** UnitOfWork interface:

```csharp
public interface IAuthUnitOfWork : IUnitOfWork
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<Teacher> Teachers { get; }
    IGenericRepository<Student> Students { get; }
    IGenericRepository<Review> Reviews { get; }   // ← add this line
}
```

### 3d. Service Implementation → `{Service}.Application/Services/{Feature}Service.cs`

Rules:
- Constructor-inject: `IUnitOfWork`, `IMapper`, `IQueryablePager` (for paging), helpers as needed
- All methods must be `async Task<CommonResponse<T>>`
- Always validate ownership: if user modifies a resource, confirm they own it
- Soft delete: set `IsDeleted = true`, `DeletedAt = DateTime.UtcNow`, then `SaveChangesAsync`
- Use `BeginTransactionAsync` / `CommitTransactionAsync` / `RollbackTransactionAsync` when writing multiple entities
- Return `CommonResponse<T>` with clear Vietnamese-friendly messages
- Always pass `cancellationToken` to `SaveChangesAsync` and async repo calls

```csharp
public class ReviewService : IReviewService
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReviewService(IAuthUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CommonResponse<ReviewResponseDto>> CreateReviewAsync(
        Guid studentId, CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<ReviewResponseDto>();

        // 1. Validate teacher exists
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(request.TeacherId);
        if (teacher == null || teacher.IsDeleted)
        {
            response.IsSuccess = false;
            response.Message = "Teacher not found";
            response.ListErrors.Add(new Errors { Field = "TeacherId", Detail = "Teacher does not exist" });
            return response;
        }

        // 2. Create entity
        var review = _mapper.Map<Review>(request);
        review.StudentId = studentId;

        await _unitOfWork.Reviews.AddAsync(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        response.Data = _mapper.Map<ReviewResponseDto>(review);
        response.Message = "Review created successfully";
        return response;
    }
}
```

### 3e. AutoMapper Profile → `{Service}.Application/Common/{Service}MappingProfile.cs`

Add mappings to the **existing** profile class:

```csharp
// Inside existing MappingProfile
CreateMap<CreateReviewRequest, Review>();
CreateMap<Review, ReviewResponseDto>();
```

---

## 4. INFRASTRUCTURE LAYER

### 4a. EF Core Entity Configuration → `{Service}.Infrastructure/Persistence/Configurations/{Entity}Configuration.cs`

```csharp
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(1000);

        builder.HasOne(r => r.Teacher)
               .WithMany()
               .HasForeignKey(r => r.TeacherId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(r => !r.IsDeleted);   // ← global soft-delete filter
    }
}
```

Register configuration in DbContext's `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new ReviewConfiguration());
```

### 4b. UnitOfWork Implementation → `{Service}.Infrastructure/Implements/Repositories/UnitOfWork.cs`

Add the new `DbSet` property to the **existing** `UnitOfWork` class:

```csharp
public IGenericRepository<Review> Reviews =>
    new GenericRepository<Review>(_context);
```

### 4c. DI Registration → `{Service}.Infrastructure/DependencyInjection/ManageDependencyInjection.cs`

Register service in the **existing** `AddScopedInterface` method:

```csharp
service.AddScoped<IReviewService, ReviewService>();
```

### 4d. EF Migration (remind user)

After infrastructure changes, remind the user to run:
```bash
cd {Service}.Infrastructure
dotnet ef migrations add Add{Entity}Table --startup-project ../{Service}.Api
dotnet ef database update --startup-project ../{Service}.Api
```

---

## 5. API LAYER (Controller)

### Controller rules
- Inherit from `ControllerBase`
- Attributes: `[ApiController]`, `[Route("api/[prefix]/[resource]")]`
- All endpoints return `IActionResult`
- All endpoints require `[Authorize]` unless explicitly public
- Role restrictions use manual claim checks (no policy strings)
- Extract user ID with: `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- Always validate ModelState with `if (!ModelState.IsValid)` and fill `ListErrors`

### HTTP verb + status code mapping

| Operation       | Verb   | Success Code |
|----------------|--------|-------------|
| Create         | POST   | 201 Created |
| Get one/list   | GET    | 200 OK      |
| Full update    | PUT    | 200 OK      |
| Partial update | PATCH  | 200 OK      |
| Delete         | DELETE | 200 OK      |

### Controller template

```csharp
[ApiController]
[Route("api/auth/reviews")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // POST api/auth/reviews
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CommonResponse<ReviewResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommonResponse<ReviewResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var response = new CommonResponse<ReviewResponseDto>();

        if (!ModelState.IsValid)
        {
            response.IsSuccess = false;
            response.Message = "Validation failed";
            foreach (var error in ModelState)
                foreach (var err in error.Value!.Errors)
                    response.ListErrors.Add(new Errors { Field = error.Key, Detail = err.ErrorMessage });
            return BadRequest(response);
        }

        var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(studentIdClaim, out var studentId))
            return Unauthorized();

        var result = await _reviewService.CreateReviewAsync(studentId, request, cancellationToken);
        return result.IsSuccess ? StatusCode(201, result) : BadRequest(result);
    }

    // GET api/auth/reviews/{id}
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CommonResponse<ReviewResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetReviewByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // DELETE api/auth/reviews/{id}
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CommonResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(studentIdClaim, out var studentId))
            return Unauthorized();

        var result = await _reviewService.DeleteReviewAsync(studentId, id, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
```

### Admin-only endpoint pattern

```csharp
[HttpGet]
[Authorize]
public async Task<IActionResult> GetAll([FromQuery] GetReviewsRequest request, CancellationToken cancellationToken)
{
    var role = User.FindFirst(ClaimTypes.Role)?.Value;
    if (role != "1")  // 1 = Admin
        return Forbid();

    var result = await _reviewService.GetAllReviewsAsync(request, cancellationToken);
    return Ok(result);
}
```

---

## 6. API GATEWAY (YARP)

When adding endpoints in a **new** controller, register them in the API Gateway:
File: `ApiGateway/appsettings.json` (or `ocelot.json` if using Ocelot)

Add the route cluster mapping for the new controller path. Follow the existing pattern already in that file.

---

## 7. RESPONSE FORMAT (MANDATORY)

**Every** endpoint must return `CommonResponse<T>`:

```json
// Success
{
  "isSuccess": true,
  "message": "Review created successfully",
  "data": { ... },
  "listErrors": []
}

// Validation failure
{
  "isSuccess": false,
  "message": "Validation failed",
  "data": null,
  "listErrors": [
    { "field": "Rating", "detail": "Rating must be between 1 and 5" }
  ]
}

// Not found / business error
{
  "isSuccess": false,
  "message": "Teacher not found",
  "data": null,
  "listErrors": []
}
```

---

## 8. WHAT NOT TO DO

- Do NOT use `int` or `long` for entity primary keys – always `Guid`
- Do NOT add methods to base entities or shared kernel classes
- Do NOT create `Repository` classes for individual entities – use `IGenericRepository<T>` via `UnitOfWork`
- Do NOT use `[Authorize(Policy = "...")]` – use manual role claim checks
- Do NOT return raw entities from controllers – always map to DTO first
- Do NOT skip `[ProducesResponseType]` attributes on controller actions
- Do NOT use `DateTime.Now` – always use `DateTime.UtcNow`
- Do NOT add docstrings/XML comments unless the user asks
- Do NOT hard delete records – always soft delete via `IsDeleted = true`
- Do NOT catch exceptions inside service methods unless handling specific business cases – let `GlobalExceptionMiddleware` handle unhandled exceptions

---

## 9. CHECKLIST BEFORE FINISHING

Before declaring done, verify:
- [ ] Entity inherits `AuditableEntity`, has `HasQueryFilter(!IsDeleted)` in EF config
- [ ] Request DTOs have validation attributes
- [ ] Response DTO has all needed fields
- [ ] Service interface is defined with all methods
- [ ] UnitOfWork interface has new repository added
- [ ] Service implementation uses `CommonResponse<T>` with clear messages
- [ ] AutoMapper profile has both `CreateMap` directions
- [ ] UnitOfWork implementation exposes the new `IGenericRepository<T>`
- [ ] Service registered as `Scoped` in `ManageDependencyInjection.cs`
- [ ] Controller has `[ApiController]`, `[Route]`, `[Authorize]`, `[ProducesResponseType]`
- [ ] ModelState validation in all POST/PUT endpoints
- [ ] Correct HTTP status codes (201 for create, 200 for others)
- [ ] Reminded user to run EF migration

---

## 10. EXECUTION STEPS (in order)

When user triggers this skill, follow these steps in order:

1. **Ask clarification questions** (Step 0) – wait for answers
2. **Read existing similar files** to match style exactly (e.g., read `BookingService.cs` before writing `ReviewService.cs`)
3. **Create Domain files** (Entity + Enum)
4. **Create Application files** (Request DTO → Response DTO → Interface → Update UnitOfWork interface → Service → Update Mapper)
5. **Create Infrastructure files** (EF Config → Update UnitOfWork impl → Register in DI)
6. **Create API Controller**
7. **Remind about migration + gateway config**
8. **Show summary** of all files created/modified
