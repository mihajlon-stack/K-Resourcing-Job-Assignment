# API Reference

Short reference for the Claims API endpoints, payloads, and expected responses.

- Base URL: shown in console on startup (e.g., https://localhost:7xxx)
- Swagger UI: {BaseUrl}/swagger
- Auth: none
- Media type: application/json
- Dates: ISO 8601 date-only (YYYY-MM-DD)

## Covers

- GET /Covers
  - 200 OK: `[CoverResponse]`

- GET /Covers/{id}
  - 200 OK: `CoverResponse`
  - 404 Not Found: when the cover does not exist

- POST /Covers
  - Body: `CreateCoverRequest`
  - 201 Created: `CoverResponse` (Location header points to `/Covers/{id}`)
  - 400 Bad Request: validation errors (ValidationProblemDetails)
  - Validation rules:
    - `StartDate >= today` (inclusive)
    - `EndDate >= StartDate`
    - `EndDate <= StartDate.AddYears(1)` (calendar-aware one year)

- DELETE /Covers/{id}
  - 204 No Content: when deleted
  - 404 Not Found: when not found

- POST /Covers/compute?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD&coverType=Yacht|PassengerShip|Tanker|...
  - 200 OK: decimal premium

Schemas:
- CreateCoverRequest
```json
{
  "startDate": "2026-01-01",
  "endDate": "2026-01-31",
  "type": "Yacht"
}
```
- CoverResponse
```json
{
  "id": "cover-1",
  "startDate": "2026-01-01",
  "endDate": "2026-01-31",
  "type": "Yacht",
  "premium": 1375.0
}
```

Examples:
- Create cover
```bash
curl -s -X POST "{BaseUrl}/Covers" \
  -H "Content-Type: application/json" \
  -d '{
    "startDate": "2026-01-01",
    "endDate": "2026-01-31",
    "type": "Yacht"
  }'
```
- Compute premium (no persistence)
```bash
curl -s -X POST "{BaseUrl}/Covers/compute?startDate=2026-01-01&endDate=2026-01-31&coverType=Yacht"
```

## Claims

- GET /Claims
  - 200 OK: `[ClaimResponse]`

- GET /Claims/{id}
  - 200 OK: `ClaimResponse`
  - 404 Not Found: when missing

- POST /Claims
  - Body: `CreateClaimRequest`
  - 201 Created: `ClaimResponse` (Location header points to `/Claims/{id}`)
  - 400 Bad Request: validation errors (ValidationProblemDetails)
  - Validation rules:
    - `DamageCost <= 100000`
    - `CoverId` must reference an existing cover (invalid id → 400, not 404)
    - `Created` must fall within `[Cover.StartDate, Cover.EndDate]` (closed interval)

- DELETE /Claims/{id}
  - 204 No Content: when deleted
  - 404 Not Found: when not found

Schemas:
- CreateClaimRequest
```json
{
  "coverId": "cover-1",
  "created": "2026-01-15",
  "name": "Storm damage",
  "type": "BadWeather",
  "damageCost": 1000.0
}
```
- ClaimResponse
```json
{
  "id": "claim-1",
  "coverId": "cover-1",
  "created": "2026-01-15",
  "name": "Storm damage",
  "type": "BadWeather",
  "damageCost": 1000.0
}
```

Notes:
- `CreatedAtAction` is wired so POSTs return proper 201 + Location to the corresponding `GetByIdAsync` routes.
- Validation exceptions are mapped globally to 400 with a standard RFC 7807 problem-details payload.
