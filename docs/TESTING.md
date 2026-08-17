# Testing

This section explains the intent and coverage of the test suite without enumerating every case. The focus is on boundary coverage, observable behavior, and failure isolation.

## Unit Tests

- PremiumCalculator
  - Covers the three-band arithmetic and the type multipliers.
  - Exercises boundaries: 0, 1, 30, 31, 180, 181 days and larger.
  - Asserts additive discount interpretation (e.g., Yacht band 3 = 8% below base, not compounded).

- CoverPeriod
  - Validates invariant enforcement (`end >= start`).
  - Verifies billable days with exclusive end-date semantics across boundaries.

- Validators (FluentValidation)
  - CreateCoverRequestValidator: `StartDate >= today` (inclusive), `EndDate >= StartDate`, `EndDate <= StartDate.AddYears(1)` including leap-year boundary.
  - CreateClaimRequestValidator: `DamageCost <= 100000`, referenced Cover must exist, and `Created` within `[StartDate, EndDate]` (closed interval). Uses a single Cover fetch in a combined rule to avoid double round-trips.

- Services
  - `CoverService` and `ClaimService` happy-path tests use a `FakeValidator<T>` so `ValidateAndThrowAsync` is exercised correctly without coupling to actual rule definitions.
  - Verify repository calls, id propagation, and that an audit entry is enqueued post-persistence.
  - Not-found paths return `null`/`false` and are mapped to 404 at the controller level (checked in integration).

- Auditing
  - `ChannelAuditQueue` drops and logs when full (non-blocking `TryWrite`).
  - `AuditBackgroundService` persists queued items; failure of one item is logged and does not prevent subsequent items from persisting (per-item try/catch).

## Integration Tests

- `ClaimsControllerTests`
  - Boots the host with `WebApplicationFactory<Program>` and issues real HTTP requests.
  - Verifies `GET /Claims` returns 200 and JSON, and a non-existent id returns 404.

## Test Infrastructure Notes

- Testcontainers spins up dependencies automatically; ensure Docker Desktop is running.
- xUnit v3 analyzer guidance is followed: async calls in tests pass `TestContext.Current.CancellationToken` where applicable.
- Minimal hand-written fakes are used where mocking frameworks can’t stub extension methods (`FakeValidator<T>`), or where capturing generic logging calls is cumbersome (`RecordingLogger`).
