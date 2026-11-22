# Foodie Radar

Multi-source restaurant review aggregator with filters by cuisine/country, taste, spice level, area, and an **"only native reviewers"** authenticity score.

- **Frontend:** React 18 + TypeScript + Vite
- **Backend:** ASP.NET Core 8 Web API + EF Core 8
- **Database:** SQLite for zero-setup local dev; PostgreSQL 16 with EF Core migrations in Docker / production
- **Deploy:** Docker Compose (postgres + api + nginx), GitHub Actions CI

> All restaurants and reviews in the seed data are fictional.

## Option A: Docker (PostgreSQL, closest to production)

Prerequisites: Docker Desktop, .NET 8 SDK (only to create the first migration).

```bash
# 1. Create the initial migration ONCE (generates backend/FoodieRadar.Api/Migrations/; commit it)
dotnet tool install --global dotnet-ef --version 8.*
cd backend/FoodieRadar.Api
dotnet ef migrations add InitialCreate
cd ../..

# 2. Secrets
cp .env.example .env          # set POSTGRES_PASSWORD and JWT_KEY (openssl rand -base64 48)

# 3. Run  ->  http://localhost:8080
docker compose up --build

# 4. Optional: import real Auckland restaurants
docker compose run --rm api import-osm --remove-demo
```

The API applies pending migrations on startup. After changing a model, add a new migration:
`dotnet ef migrations add <Name>` and rebuild. Data lives in the `pgdata` volume
(`docker compose down -v` wipes it).

Health check: `GET /health`.

## Option B: Run locally without Docker (SQLite)

Prerequisites: .NET 8 SDK, Node.js 18+.

```bash
# 1. Backend  ->  http://localhost:5080  (Swagger at /swagger)
cd backend/FoodieRadar.Api
dotnet run

# 2. Frontend ->  http://localhost:5173
cd frontend
npm install
npm run dev
```

The SQLite file `foodie.db` is created and seeded on first run. Delete it to reset the data
(also needed after model changes: SQLite mode uses `EnsureCreated` and ignores migrations).

To run the API locally against Postgres instead: uncomment the `db` port in `docker-compose.yml`,
`docker compose up db`, then set `Database__Provider=Postgres` and
`ConnectionStrings__Default=Host=localhost;Database=foodie;Username=foodie;Password=<yours>`.

## Tests

```bash
# Backend: unit tests + API integration tests (in-memory server, throwaway SQLite DB)
cd backend
dotnet test FoodieRadar.Api.Tests

# Frontend: Vitest + Testing Library
cd frontend
npm test
```

| Suite | Covers |
|---|---|
| `Unit/RestaurantAggregatorTests` | must-order / avoid logic, taste threshold, spice & price averaging, verdict text, curated rows untouched |
| `Unit/CatalogAndGeoTests` | native-origin matching, Auckland region bucketing, distance, keyword highlighting |
| `Integration/ApiTests` | health, meta, paging, filters, 404, login required, one review per user, no self-voting, one vote per user, validation |
| `frontend/src/**/*.test.ts(x)` | formatting helpers, `<Highlighted>` (incl. regex characters), query-string building, error messages |

GitHub Actions runs all of these on every push to `main`/`dev` and on every pull request.
To block merging until they pass: **Settings → Branches → Add rule** for `main` and `dev`, tick
*Require status checks to pass*, and select **Backend tests**, **Frontend tests** and **Docker build**.

## Import real Auckland restaurants (free, OpenStreetMap)

```bash
cd backend/FoodieRadar.Api
dotnet run -- import-osm                # add/update real restaurants, keep demo rows
dotnet run -- import-osm --remove-demo  # same, and delete the fictional demo rows
dotnet run                              # then start the API as usual
```

- Pulls Chinese, Sichuan, Cantonese, Japanese, Korean, Vietnamese, Thai and Malaysian places
  inside the Auckland urban bounding box from the public Overpass API (no key, no cost).
- Re-running is safe: rows are matched by OSM id and only OSM fields are updated;
  reviews, summaries and tags you added are kept.
- Region (North Shore / Central / West / East / South) is estimated from coordinates.
- Price, spice level, taste tags and summaries are empty for imported rows until reviews come in.
- Set `"SeedDemoData": false` in `appsettings.json` to stop the demo rows being created.
- Data © OpenStreetMap contributors (ODbL); the footer attribution must stay.
- Don't run the import in a tight loop: the public Overpass server is shared and rate-limited.
Vite proxies `/api` to the backend, so no CORS setup is needed in development.

## Project structure

```
backend/FoodieRadar.Api
  Models/        Restaurant, Review (+ ReviewSource enum), CuisineCatalog
  Data/          AppDbContext, DbSeeder
  Dtos/          Request/response records, Mapper (computes ratings)
  Services/      IReviewInsightService (rule-based now, LLM later)
  Controllers/   RestaurantsController, MetaController
frontend/src
  api.ts         Typed fetch client
  types.ts       Shared types
  components/    FilterPanel, RestaurantCard, RestaurantModal, ReviewForm, ui
```

## API

| Method | Path | Notes |
|---|---|---|
| GET | `/api/meta` | Cuisines, tastes, areas, reviewer origins |
| GET | `/api/restaurants` | Query: `cuisines`, `tastes`, `areas` (repeatable), `minSpice`, `nativeOnly`, `hasReviews`, `q`, `sort` = `native` \| `overall` \| `reviews`, `page`, `pageSize` (max 60). Returns `{ items, total, page, pageSize }` |
| GET | `/api/restaurants/{id}` | Detail + reviews (each flagged `isNative`) |
| POST | `/api/restaurants/{id}/reviews` | `{ author, reviewerOrigin, rating, text, recommendedDishes?, avoidDishes?, tastes?, spiceRating?, priceLevel? }`. Highlights are extracted and the restaurant profile is re-aggregated server-side |
| POST | `/api/reviews/{id}/helpful` | +1 helpful vote (one per browser until login exists) |

## Accounts (JWT)

| Method | Path | Notes |
|---|---|---|
| POST | `/api/auth/register` | `{ email, password (8+), displayName, origin }` → `{ token, user }` |
| POST | `/api/auth/login` | `{ email, password }` → `{ token, user }` |
| GET | `/api/auth/me` | Current user (Bearer token) |

- Writing a review and voting "helpful" require login. Author name and origin come from the profile,
  so reviewers can't pick a different "hometown" per review to game the native score.
- One review per user per restaurant; one helpful vote per user per review; no voting on your own review.
- Passwords are hashed with ASP.NET Core Identity's `PasswordHasher` (PBKDF2).
- Per-IP rate limits: 10/min on login/register, 30/min on review/vote writes.
- `Jwt:Key` (32+ chars) is read from config. A dev key is in `appsettings.Development.json`;
  in production set `Jwt__Key` as an environment variable or use `dotnet user-secrets`.
- The frontend keeps the token in localStorage. For a public launch, move to an httpOnly cookie.

## Map and "near me"

- List / map toggle. The map uses Leaflet with OpenStreetMap tiles (lazy-loaded), coloured by score.
- "📍 Near me" asks the browser for your location and sorts by distance (`sort=distance&lat=&lon=`).
- OSM's public tile server is fine for development; switch to a tile provider before real traffic.

## How the restaurant profile is built

`Services/RestaurantAggregator.cs` recomputes a restaurant after every new review
(skipped for hand-curated rows, `IsCurated = true`):

- **Must-order / avoid**: dishes recommended more often than flagged, and vice versa
- **Taste tags**: tags picked by at least 30% of reviewers who picked any
- **Spice / price**: rounded average of reviewers' input
- **Summary and verdict**: rule-based text (e.g. native reviewers rate it 0.5+ higher than everyone → "low scores are probably locals not used to the flavour"). Replace with an LLM later.

## How "native" scoring works

Each cuisine in `CuisineCatalog` lists its native reviewer origins (e.g. Sichuan → Sichuan, Chongqing, China).
`NativeRating` is the average rating from reviews whose `ReviewerOrigin` is in that list; `OverallRating` uses every review.

## Next steps

1. **EF Core migrations** instead of `EnsureCreated`, then PostgreSQL
2. **Auth** (JWT) so on-site reviews belong to real users
3. **Importers**: background job (`IHostedService` / Hangfire) pulling Google Places API data, within its terms (store place IDs, link back to sources)
4. **LLM insight service**: implement `IReviewInsightService` with an LLM to extract dishes, taste tags, spice level, and to infer reviewer origin from language
5. **Tests**: xUnit + `WebApplicationFactory<Program>` for API, Vitest for frontend
6. **Docker Compose** for api + web + postgres
