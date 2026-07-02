# Enterprise Order Processing System

Production-oriented order processing API built with Clean Architecture, Domain Driven Design, CQRS-style application flows, PostgreSQL, Redis, Hangfire, JWT authorization, and automated tests.

> Note: the solution targets `.NET 9`. The current local machine used during generation had only `.NET SDK 8.0.125`, so final build and test verification must be run after installing a .NET 9 SDK.

## Functional Scope

The system manages customer orders from creation through fulfillment.

Supported workflows:

- Create an order with one or more line items.
- Retrieve paginated orders with filtering and sorting.
- Retrieve a single order with items and full status history.
- Update order status through valid lifecycle transitions.
- Cancel pending orders.
- Automatically move stale pending orders to processing through a background job.
- Expose service health through `/health`.

Order lifecycle:

```text
Pending -> Processing -> Shipped -> Delivered
Pending -> Cancelled
```

Business rules enforced in the domain model:

- An order cannot be created without items.
- Item quantity must be greater than zero.
- Money cannot be negative.
- Order total is calculated from item totals.
- Only `Pending` orders can be cancelled.
- Once processing starts, cancellation is not allowed.
- Every status transition creates an `OrderStatusHistory` entry.

## Solution Structure

```text
src/
  OrderProcessing.Api              ASP.NET Core API, middleware, controllers, Swagger
  OrderProcessing.Application      Commands, queries, validators, services, abstractions
  OrderProcessing.Domain           Entities, value objects, enums, business rules
  OrderProcessing.Infrastructure   Redis, Hangfire, JWT/security, Serilog wiring
  OrderProcessing.Persistence      EF Core DbContext, configurations, repositories, seed data
  OrderProcessing.Contracts        Request/response DTOs
  OrderProcessing.Shared           Result pattern, API response, pagination, clock

tests/
  Application.Tests                Domain and validator tests
  Architecture.Tests               Clean Architecture dependency tests
  Integration.Tests                API/database integration test scaffolding

ui/
  order-processing-ui              React + Material UI operations console for API testing
```

## Architecture

The dependency direction follows Clean Architecture:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application
Api -> Persistence -> Application + Domain
```

The domain layer has no dependency on EF Core, ASP.NET Core, infrastructure, or persistence. Business invariants live in rich domain objects such as `Order`, `OrderItem`, `Money`, `Address`, and `OrderNumber`.

The application layer orchestrates use cases through command/query records and services. It depends on abstractions such as `IOrderRepository`, `IUnitOfWork`, and `ICacheService`, keeping infrastructure concerns replaceable.

Persistence implements repository and unit-of-work abstractions using EF Core and PostgreSQL. Infrastructure implements cross-cutting services such as caching, background jobs, logging, and authentication.

## Layer Responsibilities

Each class library has a focused responsibility. This keeps the system easier to test, safer to change, and closer to how large enterprise backends are maintained.

### `OrderProcessing.Domain`

Responsibility:

- Owns the core business model.
- Contains enterprise business rules and invariants.
- Has no dependency on ASP.NET Core, EF Core, Redis, Hangfire, or external services.

Important classes:

- `Order`: aggregate root for order creation, item management, status transitions, cancellation, total calculation, and status history.
- `OrderItem`: order line item with quantity, unit price, and calculated line total.
- `OrderStatusHistory`: immutable-style history record for every status change.
- `Customer`: customer entity used by the ordering domain.
- `Product`: product entity with SKU, name, price, and active state.
- `Money`: value object that prevents negative amounts and currency mismatch.
- `Address`: value object for shipping address consistency.
- `OrderNumber`: value object for order identity formatting.
- `OrderStatus`: enum for valid order states.
- `DomainException`: explicit exception type for business-rule violations.

Why this is best:

- Business rules remain independent from frameworks and databases.
- Domain behavior can be unit tested without web server, database, or infrastructure.
- The model avoids an anemic design by putting behavior inside entities, not only in services.
- Changes to PostgreSQL, Redis, or API transport do not rewrite core order rules.

### `OrderProcessing.Application`

Responsibility:

- Coordinates use cases.
- Defines commands, queries, validators, service interfaces, repository abstractions, and unit-of-work contracts.
- Translates domain behavior into application results and DTO responses.

Important classes:

- `CreateOrderCommand`, `UpdateStatusCommand`, `CancelOrderCommand`: write-side use case inputs.
- `GetOrderByIdQuery`, `GetOrdersQuery`: read-side use case inputs.
- `OrderService`: application service that validates input, calls domain methods, persists changes, manages cache invalidation, and returns `Result`.
- `CreateOrderCommandValidator`, `UpdateStatusCommandValidator`, `CancelOrderCommandValidator`: FluentValidation rules for use-case input.
- `IOrderRepository`: order-specific persistence abstraction.
- `IRepository<T>`: generic repository abstraction.
- `IUnitOfWork`: transaction/save boundary abstraction.
- `ICacheService`: cache abstraction used without depending on Redis directly.
- `IOrderProcessingJob`: background job abstraction.
- `OrderMapping`: maps domain entities to API-safe DTOs.
- `OrderProcessingOptions`: strongly typed options for background processing and caching.

Why this is best:

- Use cases are isolated from controllers and database implementation.
- Validation is centralized and reusable.
- Application logic can be tested with mocks instead of real infrastructure.
- Dependency inversion allows EF Core, Redis, and Hangfire to be replaced without changing use cases.
- `Result`-based flow keeps expected business errors separate from unexpected exceptions.

### `OrderProcessing.Contracts`

Responsibility:

- Defines API request and response DTOs.
- Prevents raw domain entities from leaking over HTTP.
- Provides stable external contracts for clients.

Important classes:

- `CreateOrderRequest`: HTTP payload for creating an order.
- `CreateOrderItemRequest`: line item payload.
- `UpdateOrderStatusRequest`: payload for status transitions.
- `CancelOrderRequest`: payload for cancellation.
- `OrderResponse`: client-facing order details.
- `OrderItemResponse`: client-facing item details.
- `OrderStatusHistoryResponse`: client-facing status history.
- `GetOrdersRequest`: query-string model for pagination, filtering, and sorting.

Why this is best:

- API contracts can evolve without exposing internal domain structure.
- Sensitive or persistence-only properties stay hidden.
- Controllers stay clean because request/response shapes are explicit.
- Frontend, mobile, and integration clients receive predictable payloads.

### `OrderProcessing.Shared`

Responsibility:

- Holds small cross-cutting primitives that are safe for multiple layers.
- Avoids duplication of generic response, result, pagination, and time abstractions.

Important classes:

- `Result` and `Result<T>`: success/failure pattern for expected application outcomes.
- `Error`: structured error code and message.
- `ApiResponse<T>`: standard API envelope with success, message, data, errors, trace ID, and timestamp.
- `PagedResult<T>`: paginated response model.
- `PaginationRequest`: normalized paging and sorting input.
- `ISystemClock` and `SystemClock`: testable time abstraction.

Why this is best:

- Common primitives are reused without coupling to infrastructure.
- Time-dependent logic can be tested deterministically.
- API responses remain consistent across endpoints.
- Pagination behavior is standardized.

### `OrderProcessing.Persistence`

Responsibility:

- Implements database access using EF Core and PostgreSQL.
- Maps domain entities and value objects to relational tables.
- Implements repositories and unit of work.
- Owns database seed logic.

Important classes:

- `OrderProcessingDbContext`: EF Core database context.
- `OrderConfiguration`: maps `Order`, owned `OrderNumber`, `Money`, `Address`, row version, indexes, items, and history.
- `OrderItemConfiguration`: maps item pricing and product fields.
- `OrderStatusHistoryConfiguration`: maps status audit history.
- `CustomerConfiguration`: maps customer data and shipping address.
- `ProductConfiguration`: maps SKU, price, and active state.
- `Repository<T>`: generic EF repository implementation.
- `OrderRepository`: order-specific queries, detailed reads, pagination, filtering, sorting, and stale-pending lookup.
- `UnitOfWork`: save boundary for EF changes.
- `DatabaseSeeder`: development seed data.

Why this is best:

- EF Core details stay out of domain and application logic.
- Query behavior is optimized close to the database layer.
- Owned value objects preserve DDD modeling while still mapping cleanly to PostgreSQL.
- Repository abstractions make application tests simpler.
- Indexes and split queries improve performance for common order reads.

### `OrderProcessing.Infrastructure`

Responsibility:

- Implements external technical capabilities.
- Wires caching, background processing, JWT authentication, authorization, and logging.

Important classes:

- `DistributedCacheService`: implements `ICacheService` using `IDistributedCache`, Redis, or memory fallback.
- `PendingOrderProcessorJob`: Hangfire job that moves stale pending orders to processing.
- `JwtOptions`: strongly typed JWT configuration.
- `DependencyInjection`: infrastructure registration for Redis, Hangfire, JWT, authorization policies, and Serilog.

Why this is best:

- External technology choices stay replaceable.
- Application code depends on interfaces, not Redis or Hangfire directly.
- Background jobs reuse the same domain rules as API workflows.
- Security and observability are configured in one place.

### `OrderProcessing.Api`

Responsibility:

- Hosts the ASP.NET Core application.
- Exposes HTTP endpoints.
- Handles authentication, authorization, middleware, Swagger, health checks, and request/response formatting.

Important classes:

- `Program`: composition root for dependency injection, middleware, Swagger, health checks, Hangfire, rate limiting, and startup seeding.
- `OrdersController`: HTTP API for creating, reading, updating, and cancelling orders.
- `GlobalExceptionMiddleware`: converts unexpected failures, business errors, database errors, and concurrency conflicts into `ProblemDetails`.
- `SecureHeadersMiddleware`: adds security-related response headers.

Why this is best:

- Controllers stay thin and delegate business work to application services.
- API concerns are not mixed into domain entities or use cases.
- Middleware centralizes exception and security behavior.
- Swagger, health checks, and operational endpoints are configured at the host boundary.

### Test Projects

Responsibility:

- Protect business rules, architecture boundaries, and integration behavior.

Projects:

- `Application.Tests`: validates domain behavior and application validators.
- `Architecture.Tests`: verifies Clean Architecture dependency rules.
- `Integration.Tests`: provides API/database integration test scaffolding.

Why this is best:

- Domain and validator tests run quickly.
- Architecture tests prevent accidental dependency leaks.
- Integration tests can grow around real PostgreSQL/Testcontainers workflows.

## Key Technical Features

- `.NET 9` ASP.NET Core Web API
- Clean Architecture and DDD
- CQRS-style command/query models
- Repository and Unit of Work patterns
- Result pattern for business/application errors
- FluentValidation for request/use-case validation
- EF Core with PostgreSQL
- Owned value-object mapping for `Money`, `Address`, and `OrderNumber`
- Optimistic concurrency through `RowVersion`
- Database indexes for order status, creation date, customer, SKU, and history queries
- Redis-compatible distributed cache
- Hangfire recurring background job
- JWT authentication and role-based authorization
- Rate limiting
- Secure response headers
- Serilog request/exception/background job logging
- Swagger/OpenAPI with JWT bearer support
- Docker and Docker Compose for API, PostgreSQL, and Redis
- GitHub Actions CI workflow

## SOLID Principles Used

The solution applies SOLID principles deliberately. The goal is not academic purity; the goal is code that is easier to test, extend, and operate in a real backend system.

### Single Responsibility Principle

Where used:

- `Order`: owns order business behavior only.
- `OrderService`: orchestrates order use cases only.
- `OrdersController`: handles HTTP concerns only.
- `OrderRepository`: handles order persistence queries only.
- `DistributedCacheService`: handles cache operations only.
- `PendingOrderProcessorJob`: handles stale pending order processing only.
- `GlobalExceptionMiddleware`: handles exception-to-response conversion only.

Why used:

- Each class has one clear reason to change.
- Business rule changes stay mostly in the domain.
- API formatting changes stay in the API layer.
- Database query changes stay in persistence.
- Background job changes stay in infrastructure.

Example:

```text
OrdersController -> validates HTTP route/body shape and delegates
OrderService     -> coordinates use case
Order            -> enforces business rule
OrderRepository  -> persists and queries data
```

### Open/Closed Principle

Where used:

- `IOrderRepository`, `IUnitOfWork`, `ICacheService`, and `IOrderProcessingJob` abstractions.
- FluentValidation validators.
- Dependency injection registration in each layer.
- Result pattern through `Result` and `Result<T>`.

Why used:

- New infrastructure implementations can be added without changing application use cases.
- Redis cache can be replaced with another distributed cache implementation.
- Repository implementation can change without rewriting `OrderService`.
- New validators or command handlers can be added with minimal changes to existing code.

Example:

```text
OrderService depends on ICacheService.
DistributedCacheService implements ICacheService.

If Redis is replaced later, OrderService does not change.
```

### Liskov Substitution Principle

Where used:

- `IRepository<T>` implementations.
- `ICacheService` implementations.
- `IOrderProcessingJob` implementation.
- `ISystemClock` implementation.

Why used:

- Application services can use abstractions without caring which concrete implementation is supplied.
- Tests can substitute mocks or fakes for repositories, cache, and clock.
- Runtime can use Redis cache while tests can use a fake cache.

Example:

```text
ISystemClock -> SystemClock in production
ISystemClock -> fixed test clock in tests
```

### Interface Segregation Principle

Where used:

- `IOrderRepository` contains order-specific operations.
- `IRepository<T>` contains generic persistence operations.
- `ICacheService` contains only cache operations.
- `IUnitOfWork` contains only save/transaction boundary behavior.
- `IOrderService` exposes only order use cases.

Why used:

- Consumers depend only on operations they actually need.
- `OrderService` does not depend on EF Core `DbContext`.
- Cache consumers do not know anything about Redis configuration.
- Background jobs do not depend on controller or HTTP abstractions.

Example:

```text
PendingOrderProcessorJob needs:
- IOrderRepository
- IUnitOfWork
- ISystemClock

It does not need:
- OrdersController
- DbContext
- HttpContext
```

### Dependency Inversion Principle

Where used:

- Application layer defines repository, cache, unit-of-work, and job abstractions.
- Persistence and infrastructure implement those abstractions.
- API composes implementations through dependency injection.

Why used:

- High-level business workflows do not depend on low-level technical details.
- Use cases remain testable without PostgreSQL, Redis, or Hangfire.
- Technical decisions can change without rewriting domain or application behavior.

Example:

```text
Application:
  IOrderRepository

Persistence:
  OrderRepository : IOrderRepository

API:
  services.AddScoped<IOrderRepository, OrderRepository>()
```

## Design Patterns Used

The system uses enterprise patterns where they add clarity, testability, or operational value.

### Clean Architecture

Where used:

- Entire solution structure.
- Dependency direction from API/infrastructure inward to application/domain.

Why used:

- Keeps business rules independent from frameworks.
- Makes the domain and application layers highly testable.
- Supports long-term maintainability as infrastructure changes.

### Domain Driven Design

Where used:

- `Order` aggregate root.
- `OrderItem`, `OrderStatusHistory`, `Customer`, and `Product` entities.
- `Money`, `Address`, and `OrderNumber` value objects.
- `OrderStatus` domain enum.
- Domain methods such as `AddItem`, `UpdateStatus`, `Cancel`, and `EnsureReadyForPlacement`.

Why used:

- Business rules live close to the data they protect.
- Invalid states are harder to create.
- The code speaks the business language of orders, items, status, cancellation, and fulfillment.

### Aggregate Root Pattern

Where used:

- `Order` is the aggregate root for order items and status history.

Why used:

- All order mutations go through `Order`.
- Order totals and status history stay consistent.
- External code cannot freely mutate child collections.

Example:

```text
Order.AddItem(...)
Order.UpdateStatus(...)
Order.Cancel(...)
```

### Value Object Pattern

Where used:

- `Money`
- `Address`
- `OrderNumber`

Why used:

- Encapsulates validation and equality for concept-specific values.
- Prevents primitive obsession.
- Makes invalid values such as negative money harder to introduce.

### Repository Pattern

Where used:

- `IRepository<T>`
- `IOrderRepository`
- `Repository<T>`
- `OrderRepository`

Why used:

- Hides EF Core query details from application services.
- Makes use cases easier to test.
- Centralizes order-specific queries such as detailed reads, pagination, filtering, sorting, and pending-order lookup.

### Unit of Work Pattern

Where used:

- `IUnitOfWork`
- `UnitOfWork`
- EF Core `SaveChangesAsync`

Why used:

- Provides a clear transaction/save boundary.
- Keeps application service methods explicit about when changes are committed.
- Supports future transaction expansion across multiple repositories.

### CQRS-style Command and Query Pattern

Where used:

- Commands: `CreateOrderCommand`, `UpdateStatusCommand`, `CancelOrderCommand`.
- Queries: `GetOrderByIdQuery`, `GetOrdersQuery`.

Why used:

- Separates write use cases from read use cases.
- Makes validation and use-case intent clearer.
- Supports future migration to MediatR or separate read models if needed.

### Result Pattern

Where used:

- `Result`
- `Result<T>`
- `Error`
- `OrderService`

Why used:

- Expected business failures are returned explicitly.
- Controllers can map failures to consistent API responses.
- Avoids using exceptions for normal validation or business-rule outcomes.

Example:

```text
Order not found       -> Result.Failure("orders.not_found")
Invalid transition    -> Result.Failure("orders.business_rule")
Successful operation  -> Result.Success(data)
```

### Specification-style Query Encapsulation

Where used:

- `OrderRepository.SearchAsync`
- `OrderRepository.GetPendingOlderThanAsync`

Why used:

- Query intent is centralized in repository methods.
- Filtering, sorting, pagination, and stale-order selection are not scattered across controllers.
- This gives the project a practical foundation for formal specification classes later.

### Options Pattern

Where used:

- `OrderProcessingOptions`
- `JwtOptions`

Why used:

- Configuration is strongly typed.
- Magic strings and direct configuration reads are reduced.
- Background job thresholds, cache duration, and JWT settings can be changed per environment.

### Dependency Injection Pattern

Where used:

- `AddApplication`
- `AddPersistence`
- `AddInfrastructure`
- ASP.NET Core service container in `Program`.

Why used:

- Keeps object creation out of business classes.
- Supports test-time replacement of dependencies.
- Makes class dependencies explicit through constructors.

### Middleware Pattern

Where used:

- `GlobalExceptionMiddleware`
- `SecureHeadersMiddleware`
- ASP.NET Core authentication, authorization, rate limiting, and Serilog request logging.

Why used:

- Cross-cutting HTTP concerns are handled consistently.
- Controllers remain focused on endpoint behavior.
- Security headers and exception formatting are applied globally.

### Background Job Pattern

Where used:

- Hangfire recurring job registration.
- `PendingOrderProcessorJob`.

Why used:

- Long-running or scheduled work is kept outside request/response flow.
- Pending order processing is reliable and retryable.
- Operational behavior is visible through Hangfire.

### Decorator-friendly Abstraction

Where used:

- Interfaces such as `IOrderService`, `IOrderRepository`, and `ICacheService`.

Why used:

- The project can later add decorators for logging, metrics, retries, validation, caching, or authorization without changing core implementations.
- This is useful in enterprise systems where observability and policies often grow over time.

### Factory Method Style

Where used:

- `Order.Create`
- `Customer.Create`
- `Product.Create`
- `Money.Create`
- `Address.Create`
- `OrderNumber.Create`
- `OrderNumber.New`

Why used:

- Centralizes construction rules.
- Prevents invalid object creation.
- Keeps constructors controlled for EF Core while still offering safe creation methods.

## API Endpoints

Base routes:

```text
POST   /orders
GET    /orders
GET    /orders/{id}
PATCH  /orders/{id}/status
DELETE /orders/{id}
GET    /health
```

Authorization policies:

- `CanReadOrders`: `Customer`, `Admin`, `Manager`
- `CanManageOrders`: `Admin`, `Manager`
- `CanCancelOrders`: `Customer`, `Admin`, `Manager`

Standard API response shape:

```json
{
  "success": true,
  "message": "Order retrieved.",
  "data": {},
  "errors": [],
  "traceId": "trace-id",
  "timestamp": "2026-07-02T10:00:00Z"
}
```

Problem responses are returned through global exception middleware using `ProblemDetails`.

## React Material UI Test Console

A premium React operations console is available at:

```text
ui/order-processing-ui
```

Purpose:

- Test protected order API endpoints from a browser.
- Search and filter paginated orders.
- Create a test order with customer, address, and item details.
- Open order details with items and status history.
- Move orders through valid status transitions.
- Cancel pending orders.
- Configure API base URL and JWT token from the UI.

Technical stack:

- React 18
- TypeScript
- Vite
- Material UI
- Lucide icons
- Typed fetch API client

The UI uses a Vite development proxy:

```text
Frontend: http://localhost:5175
Backend:  http://localhost:8080
UI calls: /api/orders -> http://localhost:8080/orders
```

Run the UI:

```bash
cd ui/order-processing-ui
npm install
npm run dev
```

Build the UI:

```bash
cd ui/order-processing-ui
npm run build
```

Before using protected endpoints, open **API Settings** in the top bar and paste a JWT token with one of these roles:

- `Customer`
- `Admin`
- `Manager`

The backend JSON configuration includes string enum serialization so the UI can send and receive statuses such as `Pending`, `Processing`, `Shipped`, `Delivered`, and `Cancelled`.

## Example Create Order Request

```http
POST /orders
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "customerId": "00000000-0000-0000-0000-000000000001",
  "shippingAddress": {
    "line1": "100 Market Street",
    "line2": null,
    "city": "San Francisco",
    "state": "CA",
    "postalCode": "94105",
    "country": "US"
  },
  "items": [
    {
      "productId": "00000000-0000-0000-0000-000000000010",
      "productSku": "SKU-LAPTOP-001",
      "productName": "Enterprise Laptop",
      "quantity": 1,
      "unitPrice": 1299.99,
      "currency": "USD"
    }
  ]
}
```

## Background Processing

Hangfire registers a recurring job named `process-pending-orders`.

Schedule:

```text
Every 5 minutes
```

Behavior:

- Finds `Pending` orders older than `OrderProcessing:PendingOrderThresholdMinutes`.
- Moves them to `Processing`.
- Writes a status history entry.
- Logs every changed order.
- Uses Hangfire retry behavior for failures.

Relevant options:

```json
{
  "OrderProcessing": {
    "PendingOrderThresholdMinutes": 15,
    "PendingOrderBatchSize": 100,
    "OrderCacheMinutes": 5
  }
}
```

## Configuration

Main configuration keys:

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=order_processing;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Issuer": "OrderProcessing",
    "Audience": "OrderProcessing.Client",
    "SigningKey": "change-this-secret"
  }
}
```

For production, provide secrets through environment variables, Key Vault, AWS Secrets Manager, Kubernetes secrets, or another secure configuration provider. Do not use the development signing key outside local development.

## Running Locally

Prerequisites:

- .NET 9 SDK
- Docker Desktop or compatible Docker runtime

Restore, build, and test:

```bash
dotnet restore OrderProcessing.sln
dotnet build OrderProcessing.sln
dotnet test OrderProcessing.sln
```

Run with Docker Compose:

```bash
docker compose up --build
```

API URL:

```text
http://localhost:8080
```

Swagger:

```text
http://localhost:8080/swagger
```

Health:

```text
http://localhost:8080/health
```

Hangfire dashboard:

```text
http://localhost:8080/jobs
```

## Testing Strategy

Current test coverage includes:

- Domain business rules for order creation, totals, cancellation, and status history.
- FluentValidation rules for create-order commands.
- Architecture dependency tests to protect Clean Architecture boundaries.
- Integration test scaffolding for API health checks.

Recommended next test additions:

- Full API integration tests using Testcontainers PostgreSQL.
- JWT-authenticated endpoint tests.
- Repository tests for pagination/filter/sort behavior.
- Hangfire job tests for stale pending order processing.
- Concurrency conflict tests for simultaneous status updates.

## Security Considerations

Implemented:

- JWT bearer authentication.
- Role-based authorization policies.
- Rate limiting.
- Input validation.
- EF Core parameterized SQL protection.
- Secure headers.
- Global exception handling without raw stack traces in API responses.

Production hardening still expected:

- Replace development JWT signing key.
- Restrict Hangfire dashboard access.
- Add HTTPS enforcement and HSTS behind the deployment gateway.
- Add centralized audit logging for status changes.
- Use managed secret storage.
- Add structured security event logs.

## Operational Notes

The API seeds demo customer and product data at startup after applying EF migrations. In a stricter production environment, migrations should be run by CI/CD or deployment automation instead of application startup.

The cache implementation uses Redis when `ConnectionStrings:Redis` is configured and falls back to distributed memory cache when Redis is absent.

## Current Verification Status

Code generation and static cleanup were completed. The local environment could not complete build/test verification because only `.NET SDK 8.0.125` was installed while the solution targets `.NET 9`.

After installing .NET 9, run:

```bash
dotnet restore OrderProcessing.sln
dotnet build OrderProcessing.sln
dotnet test OrderProcessing.sln
```
