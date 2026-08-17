# Architecture

## 1. Overview
This solution is an Insurance Claims Handling API. It lets clients create, read, and delete claims and manage covers. Core flows:
- Create/inspect covers and compute premiums.
- Create/read/delete claims tied to a cover, with validation and async auditing.

## 2. Layering
Dependencies point inward only; `Claims.Domain` references nothing.

- Claims.Domain
  - Entities and enums: `Claim`, `Cover`, `ClaimType`, `CoverType`.
  - Value type: `CoverPeriod` encapsulates billable days (exclusive end date) and guards invariants.
  - Domain logic: `PremiumCalculator` with band arithmetic and multipliers.
  - No framework/database dependencies.

- Claims.Application
  - Service and repository interfaces: `IClaimService`, `ICoverService`, `IClaimRepository`, `ICoverRepository`.
  - DTOs for requests/responses.
  - Validation with FluentValidation: `CreateCoverRequestValidator`, `CreateClaimRequestValidator`.
  - Auditing abstraction: `AuditEntry`, `IAuditQueue`.

- Claims.Infrastructure
  - Persistence: EF Core `AuditContext` for audit records; Mongo-backed `ClaimsContext` for domain data via fluent mapping.
  - Repositories: `ClaimRepository`, `CoverRepository`.
  - Auditing: bounded channel queue (`ChannelAuditQueue`) and background worker (`AuditBackgroundService`).
  - Options and DI wiring for the above.

- Claims (host)
  - Controllers: `ClaimsController`, `CoversController` (thin; return `ActionResult<T>` and map status codes).
  - Middleware: global validation-to-400 handler.
  - Composition root: `Program.cs` configures DI, controllers, and routing (keeps Async suffixes to fix `CreatedAtAction`).
  - Swagger for API exploration.

What this replaced: the original controller mixed DbContext construction, persistence, and business rules in one place; auditing was `new`-ed inside methods; premium math lived in the controller; and GET/DELETE results didn’t consistently translate to HTTP status codes. The refactor introduces testable seams and isolates concerns.

## 3. Getting Started
- Prerequisites
  - .NET SDK (targets net9.0).
  - Docker Desktop running (Testcontainers auto-provisions local SQL Server and Mongo for dev/tests).

- Run the API
  - From the `Claims` project folder: `dotnet run`.
  - Testcontainers will start required services automatically.
  - Swagger UI: the console prints the listening URLs (typically https://localhost:7xxx/swagger). Navigate there to try endpoints.

- Run tests
  - `dotnet test` (Docker must be running; containers are managed automatically).

## 4. Design Decisions
Each item states the decision, followed by the reasoning, including alternatives rejected and why.

1. **Band-3 discounts stack additively, not multiplicatively.**
   “Discounted by an additional 3%” could mean 5%+3% = 8% off base, or 0.95×0.97 = 7.85% compounded. Additive is the more natural reading, and it’s corroborated by the original buggy code, which hardcoded exactly 0.08 (Yacht) and 0.03 (other). The band structure was broken, but those constants look deliberate. The implementation matches those numbers and was verified against the original author’s expectations.

2. **Insurance period stays exclusive of the end date.**
   Inclusivity wasn’t on the brief’s list of bugs. Changing it would be an unrequested opinion, not a fix, and invites review-time confusion. It’s arbitrary either way; preserved for that reason. The rule is isolated in `CoverPeriod` so reversing it later is a one-line change.

3. **The one-year limit is calendar-aware (`StartDate.AddYears(1)`), not a fixed 365 days.**
   Validation was missing originally, so we chose the interpretation that correctly enforces “cannot exceed 1 year.” A fixed-365 check rejects legitimate one-year covers that span a leap day (e.g., 1 Jan 2028 → 31 Dec 2028 = 366 days). Boundary is inclusive — exactly one year is allowed (“cannot exceed”).

4. **Audit queue is bounded and drops on saturation.**
   The failure philosophy is “log it; never block or fail the request.” A full-queue drop is the same philosophy at an earlier point (capacity exhaustion vs DB write failure), handled consistently. Unbounded queues risk unbounded memory under load. Blocking backpressure was rejected because it reintroduces request-thread coupling.

5. **Host project keeps the name `Claims`, not `Claims.Api`.**
   Renaming touches the solution and test bootstrapping for negligible gain; the layered libraries already make the structure obvious.

6. **Claim.Created must fall in the closed interval [Cover.StartDate, Cover.EndDate].**
   This is separate from billing’s exclusive end: billing answers “how many days to charge,” while this answers “was the incident covered.” An incident on the last day should be covered, even if that day isn’t billable under the billing convention. The check uses the raw dates, not `CoverPeriod`, to avoid inheriting the billing convention implicitly.

7. **Services throw for validation, return null for not-found.**
   The original issue was the missing translation to HTTP status codes, not the null-return itself. Validation throws because it carries structured error data suitable for a global handler; not-found is represented by null/false and translated to 404 at the controller layer. `DELETE` on a missing id returns 404 to match `GET` behavior. A missing Cover id inside a POST body is a validation (400) problem, not a URL-addressed (404) miss.

8. **Claim validation uses one combined rule with a single Cover fetch.**
   Existence and date-containment share the same data, so one async rule loads once and short-circuits on absence, avoiding double round-trips and TOCTOU gaps.

9. **`TimeProvider` is injected instead of using `DateTime.UtcNow`.**
   The “not in the past” rule and audit timestamps depend on wall-clock time. Injecting a clock makes these tests deterministic. One clock is used across the path (validators and auditing) to avoid inconsistencies. `StartDate == today` is valid (inclusive, matching the one-year rule’s reading).

## 5. Implementation Notes
Correctness requirements called out explicitly so they aren’t lost in translation:
- Call `ValidateAndThrowAsync`, not `ValidateAsync` — otherwise invalid data can slip through.
- The audit worker wraps persistence per dequeued item, not the outer loop; exceptions must not stop the host or silently kill the worker.
- Audit enqueue happens only after the primary entity is confirmed persisted (no ghost audit records).
- On shutdown, remaining queue items are dropped; this is consistent with the best-effort audit policy.
