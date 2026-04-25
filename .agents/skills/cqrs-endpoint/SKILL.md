---
name: cqrs-endpoint
description: >
  Use when creating a new API endpoint in the Homeless project. This skill guides the full workflow:
  writing tests first (TDD), generating a filter value object when needed, creating the command or query
  record, writing a FluentValidation validator, implementing the handler with the correct read/write
  repository and tenant context, mapping with Riok.Mapperly, and wiring up the controller action.
  Trigger whenever the user asks to add an endpoint, create a new use case, add a feature, implement
  a new handler, or expose a new API route — even if they don't say "CQRS" explicitly.
---

# CQRS Endpoint Skill

This skill walks you through the full slice of work needed to add one endpoint to the Homeless API.
Each step builds on the previous one, and every piece of production code must have a test written first.

---

## Orientation: What Are We Building?

Before starting, clarify two things:

1. **Command or Query?**
   - **Command** — mutates state (create, update, delete, publish). Uses `IXxxWriteRepository` + `IUnitOfWork`. Returns `Result<T>` with `Result.Created(...)` or `Result.Success(...)`.
   - **Query** — reads state only. Uses `IXxxReadRepository`. May need a `Filter` value object if the caller passes search criteria.

2. **Domain** — which aggregate/entity is this about? (`Properties`, `Tenants`, `Accounts`, `Transactions`, `Addons`, …). This determines the subfolder and which repositories/mappers to extend.

If either answer is unclear, ask the user before proceeding.

---

## Step 1: TDD First

**Before writing any production code, write the test.** Consult the `test-driven-development` skill for the Red-Green-Refactor cycle. Do not skip this step.

For each endpoint, write **two kinds of tests**:

### Unit test — handler behaviour
Location: `src/API/Homeless.Tests/Homeless.UnitTests/Application/{UseCaseName}HandlerTests.cs`

- Mock repositories with **NSubstitute** (`Substitute.For<IXxxRepository>()`)
- Use **FluentAssertions** for assertions (`result.IsSuccess.Should().BeTrue()`)
- Use **xUnit** (`[Fact]` or `[Theory]`)
- Construct the handler under test directly (no DI container needed)
- Test: success path, validation-driven rejections (e.g. `Result.Forbidden`, `Result.NotFound`), edge cases

### Integration test — HTTP endpoint
Location: `src/API/Homeless.Tests/Homeless.IntegrationTests/Scenarios/{Domain}EndpointTests.cs`

- Use `CustomWebApplicationFactory` (already wired up in `Fixtures/`)
- Make real HTTP calls via `factory.CreateClient()`
- Assert HTTP status codes and response body shape
- Keep it black-box: test the contract, not the internals

**Watch each test fail before writing production code.** A test that passes immediately is not a useful test.

See `references/test-examples.md` for concrete patterns.

---

## Step 2: Filter Value Object (Queries only)

If the query accepts multiple optional search criteria, encapsulate them in a `Filter` value object in:
`src/API/Homeless.Domain/ValueObjects/{Domain}Filter.cs`

```csharp
// Lean record — only carry fields the query actually needs
public sealed record PropertyFilter(
    string? City = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinBedrooms = null,
    PropertyType? PropertyType = null,
    ListingType? ListingType = null,
    PropertyStatus? Status = null);
```

Why a value object instead of individual parameters? It keeps the query record clean, lets the validator and repository agree on the same shape, and makes it trivial to add new criteria later without touching every signature.

Map from the DTO request to the filter in the Mapperly mapper (see Step 6).

Skip this step for simple point-lookups (e.g. `GetPropertyByIdQuery` — no filter needed).

---

## Step 3: Command or Query Record

Location: `src/API/Homeless.Application/UseCases/{Domain}/{Name}Command.cs` or `{Name}Query.cs`

Rules:
- `sealed record` implementing `IRequest<Result<T>>`
- For **commands**: add `[RequireRole(...)]` if the caller role must be restricted. Available roles: `Roles.TenantAdmin`, `Roles.Agent`, `Roles.SuperAdmin`.
- Do **not** include `TenantId` as a parameter — the handler reads it from `ITenantContext`.
- Group related mutable fields into a shared DTO (e.g. `PropertyWriteData`) rather than having 10+ inline parameters.
- For queries with pagination, include `int Page = 1, int PageSize = 20` defaults.

```csharp
// Command example
[RequireRole(Roles.TenantAdmin, Roles.Agent)]
public sealed record CreatePropertyCommand(
    PropertyWriteData Data,
    string Country = "BR"
) : IRequest<Result<PropertyResponse>>;

// Query example
public sealed record SearchPropertiesQuery(
    PropertyFilter Filter,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedResponse<PropertyResponse>>>;
```

---

## Step 4: Validator (when input needs it)

Location: `src/API/Homeless.Application/Validators/{Name}Validator.cs`

Not every command needs a validator — only add one when there are rules to enforce. FluentValidation validators are automatically wired into the `ValidationBehavior<,>` pipeline, so they run before the handler without any extra plumbing.

```csharp
public sealed class CreatePropertyValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyValidator()
    {
        RuleFor(x => x.Data.Title)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Data.Price)
            .GreaterThanOrEqualTo(0);
    }
}
```

**Tenant-level rules**: if you need to check, say, a uniqueness constraint scoped to the tenant, inject `ITenantContext` and the relevant read repository into the validator constructor, then use `MustAsync`.

Validation failures surface as `Result.Invalid(...)` via the pipeline — no need to handle them manually in the handler.

---

## Step 5: Handler

Location: `src/API/Homeless.Application/UseCases/{Domain}/{Name}Handler.cs`

The handler contains the business logic. Key patterns:

### Read vs Write repositories
- **Query handlers** inject `IXxxReadRepository` only — no write repo, no `IUnitOfWork`.
- **Command handlers** inject both `IXxxWriteRepository` **and** `IUnitOfWork`. Always call `await unitOfWork.SaveChangesAsync(cancellationToken)` before returning.
- Some command handlers also need a read repo (e.g. to check an entitlement or verify existence before mutating).

### Tenant scoping
`ITenantContext` is resolved from the DI container by the middleware — inject it and pass `.TenantId` to every repository call that needs it. Never read the tenant from the request body.

```csharp
var (items, total) = await _readRepo.SearchAsync(
    tenantId: _tenantContext.TenantId,
    filter: request.Filter,
    page: request.Page,
    pageSize: request.PageSize,
    cancellationToken: cancellationToken).ConfigureAwait(false);
```

### Result types
| Outcome | Return value |
|---------|-------------|
| Success (read) | `Result.Success(value)` |
| Success (create) | `Result.Created(value)` |
| Not found | `Result.NotFound()` |
| Auth denied | `Result.Forbidden("reason")` |
| Business rule | `Result.Error("message")` |

Map the domain entity to the response DTO using the Mapperly mapper (see Step 6) before wrapping in `Result`.

See `references/handler-examples.md` for full command and query handler examples.

---

## Step 6: Mapperly Mapper

Location: `src/API/Homeless.Application/Mappers/{Domain}Mapper.cs`

The mappers are `static partial` classes annotated with `[Mapper]`. Add new mappings as extension methods or partial methods.

```csharp
[Mapper]
public static partial class PropertyMapper
{
    // DTO → domain filter
    public static PropertyFilter ToFilter(this PropertySearchRequest r) =>
        new(r.City, r.MinPrice, r.MaxPrice, r.MinBedrooms, r.PropertyType, r.ListingType, r.Status);

    // DTO → value object (for commands)
    public static PropertyWriteData ToWriteData(this CreatePropertyRequest r) => ...;

    // Domain entity → response DTO (Mapperly source-generates the body)
    [MapperIgnoreSource(nameof(Property.DomainEvents))]
    public static partial PropertyResponse ToResponse(this Property property);
}
```

Guidelines:
- Prefer source-generated `partial` methods for entity → response mappings — Mapperly ensures compile-time safety.
- Use explicit hand-written methods for request → value object conversions where the mapping logic is non-trivial.
- Avoid mapping in the handler — always delegate to the mapper.

---

## Step 7: Controller Action

Location: `src/API/Homeless.API/Controllers/{Domain}Controller.cs`

The controller is intentionally thin — it just translates HTTP into a MediatR send.

```csharp
/// <summary>Creates a new property listing.</summary>
[HttpPost]
[ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ExpectedFailures(ResultStatus.Invalid, ResultStatus.Forbidden)]
public async Task<Result<PropertyResponse>> Create(
    [FromBody] CreatePropertyRequest request,
    CancellationToken cancellationToken) =>
    await mediator.Send(
        new CreatePropertyCommand(request.ToWriteData(), request.Country),
        cancellationToken);
```

Conventions:
- Use `[TranslateResultToActionResult]` (already on the class) + `[ExpectedFailures(...)]` to map `Result` statuses to HTTP codes automatically — don't write `if (!result.IsSuccess)` branches.
- Controllers take the HTTP request DTO (`CreatePropertyRequest`), map to command/query via the Mapper, and return `Result<T>`. That's it.
- Declare all possible `[ProducesResponseType]` attributes for OpenAPI accuracy.
- If the action is read-only, add `[AllowAnonymous]` or a specific `[Authorize(Policy = ...)]` where appropriate.

---

## Checklist

Before calling the work done, verify every item:

- [ ] Unit test written and watched fail before handler existed
- [ ] Integration test written and watched fail before controller action existed
- [ ] All tests pass (`dotnet test`)
- [ ] Filter value object created if query has search criteria
- [ ] Command/Query record is a `sealed record`
- [ ] Validator added if there are input rules to enforce
- [ ] Handler injects only the repositories it actually needs (no unnecessary write repos in query handlers)
- [ ] `tenantContext.TenantId` passed to every tenant-scoped repository call
- [ ] `unitOfWork.SaveChangesAsync()` called in command handlers before returning
- [ ] Mapperly mapper updated (no manual `new ResponseDto { ... }` in handler code)
- [ ] Controller action ≤ 5 lines, delegates entirely to MediatR
- [ ] `[ExpectedFailures]` attribute matches the failure `ResultStatus` values the handler can return
- [ ] No TenantId in the command/query parameter list

---

## Reference Files

For concrete code examples, read the relevant reference:

- `references/test-examples.md` — unit test and integration test patterns
- `references/handler-examples.md` — complete command handler and query handler examples
