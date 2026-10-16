# Verus Companies

This repo holds a company registry: a **.NET 10 Web API** with a small **Angular 21** frontend.
Clients submit a company (name and website). The API validates it, including a check that the name is
**relevant** to the website, and keeps it in memory. Companies can then be listed, filtered and searched by relevance.

- Backend: Clean Architecture with vertical slices, FastEndpoints, Mediator, FluentValidation, ErrorOr, EF Core InMemory and Serilog.
- Frontend: standalone, zoneless and OnPush. RxJS handles the data streams and signals hold the UI state.
- Tooling: central NuGet package management with lock files, pnpm with strict policies, analyzers with
  warnings as errors, CSharpier, ESLint and Prettier. A local CI script runs through git hooks (Lefthook) and GitHub Actions runs the same script.

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0.x (pinned in `backend/global.json`) |
| Node.js | 22.12+ (LTS) |
| pnpm | 10.27 — run `corepack enable` once and the version comes from `package.json` |

## Run it

```bash
# 1. Install JS dependencies (also installs the git hooks)
corepack enable
pnpm install

# 2. Backend → http://localhost:5000 and https://localhost:7000
cd backend/src/Verus.Api
dotnet run
#   API docs (Scalar): http://localhost:5000/scalar/v1
#   OpenAPI JSON:      http://localhost:5000/swagger/v1/swagger.json

# 3. Frontend → http://localhost:4200 (a second terminal)
cd frontend
pnpm start
```

The Angular dev server proxies `/api` to `http://localhost:5000` (`frontend/proxy.conf.json`), so the
browser makes same-origin calls. CORS is still enabled for `http://localhost:4200` so other clients can call the API directly.

To exercise the API without the UI, use `backend/src/Verus.Api/Companies.http` (VS Code REST Client / Rider /
Visual Studio), Postman, or curl:

```bash
curl -i -X POST http://localhost:5000/api/v1/companies \
  -H "Content-Type: application/json" \
  -d '{"name":"Microsoft","websiteUrl":"https://www.microsoft.com"}'

curl "http://localhost:5000/api/v1/companies?q=micro"
```

## Quality gates

| Command | What it does |
|---|---|
| `pnpm run ci` / `bash scripts/ci.sh` / `./scripts/ci.ps1` | Full local CI: locked restore, format check, analyzer build (warnings as errors), all tests, FE lint, format, test, build and audit |
| `cd backend && dotnet test` | ~150 backend tests: domain, application, HTTP integration and architecture rules |
| `cd frontend && pnpm test` | Frontend unit and component tests (Vitest) |

Git hooks: **pre-commit** checks formatting and lint on staged files, **commit-msg** enforces Conventional Commits,
and **pre-push** runs the full local CI.

## API

Base path: `/api/v1`. Errors are [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) problem details
(`application/problem+json`) in one shape for every failure:

```json
{
  "type": "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1",
  "title": "Validation failed",
  "status": 400,
  "detail": "Company name is required.",
  "instance": "/api/v1/companies",
  "traceId": "0HN…",
  "errors": [{ "name": "name", "reason": "Company name is required.", "code": "Company.Name.Required" }]
}
```

| Method & route | Success | Errors |
|---|---|---|
| `POST /companies` body `{ name, websiteUrl }` | **201** + `Location` + company | **400** invalid input or name not relevant · **409** domain already registered |
| `GET /companies/{id}` | **200** company | **400** malformed id · **404** unknown id |
| `GET /companies?name=&domain=&q=&sort=&page=&pageSize=` | **200** page (empty `items` when nothing matches) | **400** invalid parameters |

**Query parameters for `GET /companies`** (all optional; no parameters means "all companies"):

| Param | Meaning |
|---|---|
| `name` | Partial or full name, case-insensitive |
| `domain` | `example.com` matches the domain and its subdomains. `example` matches any host containing it. A pasted URL also works. |
| `q` | Free-text relevance search over name and domain. Results are ordered by relevance and each carries a `relevanceScore`. |
| `sort` | `relevance` (the default when `q` is set), `name` (the default otherwise), `-name`, `createdAt`, `-createdAt` |
| `page`, `pageSize` | 1-based paging, page size 1–100 (default 20). The response includes `totalCount`, `totalPages`, `hasNextPage` and `hasPreviousPage`. |

Route and query design choices:
- **One collection resource** with query filters instead of `/search` sub-routes: "retrieve all", filtering
  and searching are the same operation with different constraints.
- **Versioning in the path** (`/v1`) so it's visible and cache-friendly.
- **Uniform envelope** for collections: a page object rather than a bare array, so paging metadata travels with the data.

## Validation and the relevance strategy

1. **Input validation** sits in the domain value objects (`CompanyName`, `WebsiteUrl`). FluentValidation validators
   call them, so each rule lives in exactly one place and every field error comes back in one response.
   - Name: required, 3–200 characters, must contain letters or digits, whitespace normalized.
   - URL: absolute `http`/`https`, no credentials, not an IP address. The host must be a real registrable domain per the
     [Public Suffix List](https://publicsuffix.org/), which is embedded so no runtime I/O is needed. So `shop.example.co.uk` → `example.co.uk`.
2. **Relevance** (`NameDomainMatcher`, then `ContentMatcher`): explainable, deterministic and offline first.
   - Normalize the name: fold diacritics, lowercase, `&` → `and`, and strip legal suffixes (`Inc`, `LLC`, `GmbH`, `S.A.`…).
   - Compare it with the domain **label** (`example` in `www.example.co.uk`). Each rule produces evidence and the strongest wins:

     | Evidence | Score | Example |
     |---|---|---|
     | Exact match (squashed) | 1.00 | Bank of America ↔ bankofamerica.com |
     | Domain contains the name, or the name starts with the domain | 0.90 | Acme ↔ getacme.io · Delta Air Lines ↔ delta.com |
     | Domain contains every word of the name | 0.85 | |
     | Acronym / partial acronym | 0.85 | International Business Machines ↔ ibm.com · The New York Times ↔ nytimes.com |
     | Jaro-Winkler similarity | 0–0.95 | Mikrosoft ↔ microsoft.com |

   - If the domain evidence is below the **threshold (0.8, configurable)**, the API fetches the home page and looks for the
     name in `og:site_name`, `application-name` and `<title>` (e.g. Hacker News ↔ news.ycombinator.com).
     The fetch is SSRF-guarded: every resolved IP is checked at connect time, so private, loopback, link-local and metadata
     addresses are refused, including after redirects. It is also time-, size- and redirect-limited, retried at most once, and a
     failure only means "no evidence"; it never causes a 500.
   - A rejection's message explains why, e.g. *"Domain 'amazon.com' does not resemble 'Apple' (best match 46%)."*
   - Settings: `Relevance:Threshold` and `Relevance:EnableContentInspection` in `appsettings.json`.

Trade-offs: domain matching can't link brands to unrelated domains on its own (Alphabet ↔ abc.xyz). Content inspection
covers some of those, but many large sites block bots (abc.xyz returns 403). A production version could add a
curated alias list or a registry lookup (e.g. WHOIS / Clearbit / Wikidata) as another strategy in the same chain.

## Architecture

```
backend/src
  Verus.Domain          Company aggregate, value objects, errors, pure relevance + search scoring
  Verus.Application     Features/Companies/{CreateCompany, GetCompanyById, SearchCompanies}
                        each slice = command/query + validator + handler; ports in Abstractions/;
                        Mediator pipeline: LoggingBehavior → ValidationBehavior → handler
  Verus.Infrastructure  EF Core (InMemory) AppDbContext + repository, HTTP content provider + SSRF guard
  Verus.Api             FastEndpoints (thin: bind → mediator.Send → map ErrorOr to HTTP), Serilog, OpenAPI
backend/tests           Domain, Application (NSubstitute), Api integration (WebApplicationFactory), Architecture (NetArchTest)

frontend/src/app
  core/                 API base URL token, HTTP error interceptor → typed ApiError
  shared/               RequestState<T> union + operator, Alert, Pager
  features/companies/   data-access/ (API + DTO types) · ui/ (presentational) · feature/ (containers) · routes
```

Key decisions:
- **Dependency rule** enforced by tests: Domain → nothing; Application → Domain; Infrastructure → Application; Api composes.
  Endpoints never touch repositories or EF.
- **Expected failures are values** (`ErrorOr`), not exceptions. Exceptions mean bugs and become a logged 500 with no internals leaked.
- **Persistence:** EF Core InMemory behind `ICompanyRepository`. Storage types are internal to Infrastructure, value objects
  are mapped as owned types, and relational metadata (tables, columns, a unique index on domain) is already declared.
  To move to SQL, swap `UseInMemoryDatabase` for e.g. `UseNpgsql` in `AddInfrastructure` and add a migration.
  `DbContext` is already the unit of work, so no extra UoW abstraction is needed.
- **Relevance search** ranks in memory over the filtered candidates. A database would replace that with full-text or
  trigram search behind the same port. Plain filtering, sorting and paging already run in the store.
- **Mediator** (source-generated, MIT) rather than MediatR: same programming model, no reflection, free license.
- **Frontend state**: no store library. Each container composes RxJS streams (`debounceTime`, `switchMap`,
  `exhaustMap`) into a `RequestState<T>` discriminated union and exposes it as a signal.

## Extensibility

- **New operation** (e.g. `PUT`/`DELETE`): add one slice in `Application/Features/Companies/<UseCase>/` and one
  endpoint class. Validation, logging and error mapping come from the pipeline.
- **Pagination and sorting** already exist. New sort keys are one enum value plus one `switch` arm.
- **New relevance strategy**: add a matcher in `Domain/Relevance` and chain it in `RelevanceEvaluator`.
- **New API version**: register the endpoint with `Version(2)` and v1 keeps working.

## Repository conventions

See [AGENTS.md](AGENTS.md) for contributor and agent guidance, and [TODO.md](TODO.md) for the implementation plan and notes.
