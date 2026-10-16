# Verus Company API + Angular Frontend — Implementation Plan

## Context
Coding exercise (`Coding Exercise Angular .Net.pdf`): a .NET Web API + Angular app to submit companies
(name + website URL), validate them (incl. name↔URL **relevance**), store them in memory, and query them
(all, by id, filter by name/domain, relevance-ranked search). The requirements are fixed. What gets
evaluated is API design, validation, error handling, clean architecture, tests and FE↔BE integration.
The working dir is empty and not a git repo yet.

The plan is written so any agent can pick it up. **First implementation step: copy this file into the repo as
`TODO.md`** and tick checkboxes as work lands. Commit after each phase.

### Decisions (fixed, do not re-litigate)
| Area | Decision |
|---|---|
| Backend runtime | .NET 10 (LTS), SDK 10.0.402 installed, pinned via `global.json` (`rollForward: latestFeature`) |
| Solution | `Verus.slnx`, **Central Package Management** (`Directory.Packages.props`) |
| API | **FastEndpoints 8.3** (REPR, `v1` version group, `/api/v1/...`), ProblemDetails (RFC 9457) |
| App layer | **Mediator 3.x** (martinothamar, MIT, source-gen) + pipeline behaviors (validation, logging) |
| Validation | FluentValidation 12 in Application (input shape); domain invariants in value objects |
| Results | **ErrorOr** for expected failures (no exceptions for control flow) |
| Persistence | EF Core 10 **InMemory** provider behind `ICompanyRepository`; DbContext internal to Infrastructure. Swapping it for SQL = changing the provider + migrations |
| No UoW | `DbContext` is the UoW; repository calls `SaveChangesAsync` |
| Relevance | Domain matching first, then a homepage content-inspection fallback (SSRF-guarded), explainable score |
| Logging | Serilog (console, structured) + `LoggerMessage` source-gen for key events |
| Tests | xUnit v3, Shouldly, NSubstitute, `WebApplicationFactory` (FastEndpoints.Testing), NetArchTest for layer rules |
| Frontend | **Angular 21** (latest LTS tag `v21-lts`), standalone components, zoneless, Node 22 (installed v22.20 satisfies `^22.12`) |
| FE package mgr | **pnpm** via corepack, strictly enforced |
| FE state | RxJS for data/async streams (search `debounceTime`/`switchMap`), signals for local UI state (`toSignal` at the template edge) |
| FE tests | Vitest (Angular 21 default) + Angular Testing Library; Playwright smoke e2e optional |
| Hooks/CI | **Lefthook** (runs both stacks) + `scripts/ci.ps1`/`ci.sh` local CI + GitHub Actions mirroring it |

Influences: Milan Jovanović / Zoran Horvat (clean arch, vertical slices, value objects, no primitive obsession),
Derek Comartin (slices own their logic, aggregates guard invariants), Okhravi (model with types),
Schwarzmüller / Pocock / Morony (standalone, smart/dumb split, strict types, declarative RxJS).

---

## Repo layout
```
/AGENTS.md  /CLAUDE.md (→ "see AGENTS.md")  /README.md  /TODO.md
/.editorconfig /.gitattributes /.gitignore /lefthook.yml
/.github/workflows/ci.yml   /scripts/ci.ps1 /scripts/ci.sh
/backend
  global.json  Verus.slnx  Directory.Build.props  Directory.Packages.props  nuget.config  .config/dotnet-tools.json
  src/Verus.Domain            # Company aggregate, value objects, domain errors, relevance scoring (pure)
  src/Verus.Application       # Features/Companies/{CreateCompany,GetCompanyById,SearchCompanies}/ (command|query, handler, validator, response DTO)
                              # Abstractions/ (ICompanyRepository, IWebsiteContentProvider), Behaviors/, Paging/
  src/Verus.Infrastructure    # Persistence/ (AppDbContext, CompanyConfiguration, CompanyRepository), Relevance/ (HttpWebsiteContentProvider), DependencyInjection.cs
  src/Verus.Api               # Endpoints/Companies/{Create,GetById,Search}.cs, ErrorOr→ProblemDetails mapping, Program.cs, launchSettings (5000/7000)
  tests/Verus.Domain.Tests  tests/Verus.Application.Tests  tests/Verus.Api.IntegrationTests  tests/Verus.ArchitectureTests
/frontend   (Angular 21 workspace, pnpm)
  src/app/core/          # http error interceptor, ProblemDetails parsing, API base-url token, app config
  src/app/shared/ui/     # dumb reusable bits (alert/message, empty-state)
  src/app/features/companies/
      data-access/       # companies.api.ts (HttpClient), company.models.ts (DTO types mirroring API)
      ui/                # company-form, company-table, company-search-bar (presentational, inputs/outputs)
      feature/           # companies-page (smart: wires form + search + list)
      companies.routes.ts
```
Dependency rule: Api → Application → Domain; Infrastructure → Application/Domain; Api references Infrastructure only for DI composition. NetArchTest enforces it.

---

## Phase 0 — Project setup & tooling  ☐
**Repo**
- [ ] `git init`, `main` branch, `.gitignore` (dotnet + node), `.gitattributes` (`* text=auto eol=lf`), root `.editorconfig` (C# rules + 2-space TS/JSON/HTML).
- [ ] `AGENTS.md`: architecture, dependency rule, slice recipe ("how to add a feature"), commands (build/test/lint/format/ci), conventions (naming, ErrorOr, no business logic in endpoints, DTO-only contracts, Conventional Commits). `CLAUDE.md` just points to it.
- [ ] `TODO.md` = this plan.

**Backend tooling and package management**
- [ ] `global.json` pins SDK 10.0.x.
- [ ] `Directory.Build.props`: `net10.0`, `Nullable=enable`, `ImplicitUsings`, `LangVersion=latest`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `GenerateDocumentationFile` (Api), `RestorePackagesWithLockFile=true`, `RestoreLockedMode` when `CI=true`, `NuGetAudit=true` / `NuGetAuditMode=all` / `NuGetAuditLevel=low`, warnings-as-errors for NU1901-NU1904.
- [ ] `Directory.Packages.props`: `ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true`, and every version lives here (no `Version=` in csproj). Analyzers go in as `GlobalPackageReference`: Meziantou.Analyzer, SonarAnalyzer.CSharp.
- [ ] `nuget.config`: `<clear/>` + nuget.org only + `packageSourceMapping` (`*` → nuget.org).
- [ ] Local tool manifest: `csharpier` 1.3 (formatter, `dotnet csharpier check .` in CI).
- [ ] Create `Verus.slnx` and the 4 src + 4 test projects, all wired with project references.

**Frontend tooling and package management**
- [ ] `corepack enable`, then `pnpm dlx @angular/cli@21 new frontend --package-manager=pnpm --style=css --ssr=false --zoneless --routing`.
- [ ] `package.json`: `"packageManager": "pnpm@<latest 10.x>"`, `"engines": { "node": ">=22.12 <23 || >=24", "pnpm": ">=10" }`, `"preinstall": "npx only-allow pnpm"`.
- [ ] `.npmrc`: `engine-strict=true`, `save-exact=true`, `strict-peer-dependencies=true`, `auto-install-peers=false`.
- [ ] `pnpm-workspace.yaml` settings: `minimumReleaseAge: 1440` (supply-chain delay), `onlyBuiltDependencies: [esbuild, lmdb, ...]` (explicit allow-list for install scripts). Commit `pnpm-lock.yaml`, and CI uses `pnpm install --frozen-lockfile`. Add `pnpm audit --prod` to CI.
- [ ] `tsconfig`: Angular strict + `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`, `noImplicitOverride`, strict templates.
- [ ] ESLint (`ng add angular-eslint`, flat config, typed rules, `@angular-eslint/template` accessibility rules, `prefer-on-push`, no `any`). Prettier + `eslint-config-prettier`.

**Local CI and hooks**
- [ ] `scripts/ci.ps1` + `ci.sh`: backend `restore --locked-mode → csharpier check → build -warnaserror → test (coverage)`. Frontend `pnpm install --frozen-lockfile → lint → prettier --check → test --watch=false → build`.
- [ ] `lefthook.yml`: **pre-commit** = csharpier + prettier + eslint on staged files (fast). **commit-msg** = commitlint (Conventional Commits). **pre-push** = full `scripts/ci`. Install Lefthook as a frontend devDependency plus `lefthook install` in a `prepare` script (the root `package.json` is a tiny pnpm root only if needed; otherwise run from `/frontend` with `--cwd ..`).
- [ ] `.github/workflows/ci.yml` runs the same script on ubuntu (setup-dotnet 10, pnpm/action-setup, node 22).
- [ ] ✅ Exit criterion: empty solution + fresh Angular app pass `scripts/ci` and the hooks fire.

## Phase 1 — Domain model (Okhravi / Horvat)  ☐
- [ ] `CompanyId` (readonly record struct over Guid v7, `Guid.CreateVersion7()`).
- [ ] `CompanyName` value object: trimmed, not empty, length 3–200 ("at least a few characters"). `static ErrorOr<CompanyName> Create(string)`.
- [ ] `WebsiteUrl` value object: absolute `http/https` `Uri`, host has a dot, no userinfo, normalized (lowercase host). Exposes `Host` and `RegistrableDomain` (via `Nager.PublicSuffix`, e.g. `shop.example.co.uk` → `example.co.uk`).
- [ ] `Company` aggregate: private ctor, `static ErrorOr<Company> Create(CompanyName, WebsiteUrl, RelevanceAssessment, TimeProvider)`. The aggregate refuses creation if the assessment isn't relevant, so there's no invalid Company. Has `CreatedAt`.
- [ ] `DomainErrors.Company.*` (codes like `Company.Name.TooShort`, `Company.Website.Invalid`, `Company.NameNotRelevantToWebsite`) with concise, user-friendly messages.
- [ ] **Relevance (pure, in Domain)**: `CompanyNameNormalizer` (lowercase, remove diacritics, strip legal suffixes inc/llc/ltd/corp/gmbh/sa/co, punctuation, `&`→and), plus `NameDomainMatcher` producing `RelevanceAssessment(Score 0..1, Method, Reason)`. Rules, best score wins:
  exact squashed match (`"Acme Corp"` ↔ `acme.com`) = 1.0; domain label contains all name tokens / name contains domain label = 0.9; acronym (`International Business Machines` ↔ `ibm.com`) = 0.85; Jaro-Winkler similarity of squashed strings (F23.StringSimilarity). Threshold `0.8`, configurable via `RelevanceOptions`.
- [ ] Unit tests: table-driven `[Theory]` for each value object and matcher rule, plus negative cases.

## Phase 2 — Application layer (vertical slices)  ☐
- [ ] Abstractions: `ICompanyRepository { AddAsync, GetByIdAsync, SearchAsync(CompanySearchCriteria) }` (returns domain objects, never EF types), `IWebsiteContentProvider { Task<WebsiteMetadata?> GetAsync(Uri, ct) }`, `IRelevanceEvaluator`.
- [ ] `RelevanceEvaluator` (strategy chain): domain matcher, then on failure the content strategy (fetch title / `og:site_name` / `application-name`, then fuzzy match against those) → `RelevanceAssessment` with method `Domain` | `Content` and reason.
- [ ] Shared: `PagedRequest(page=1,pageSize=20, max 100)`, `PagedResponse<T>(items,page,pageSize,totalCount)`.
- [ ] Behaviors: `ValidationBehavior<TReq,TRes>` (FluentValidation → `ErrorOr` validation errors, no throwing) and `LoggingBehavior` (start/end/failure with request name + elapsed).
- [ ] Slices (each folder = command/query + validator + handler + response DTO):
  - `CreateCompany` → `ErrorOr<CompanyResponse>`; logs created/rejected (with relevance reason).
  - `GetCompanyById` → `ErrorOr<CompanyResponse>` (`Error.NotFound`).
  - `SearchCompanies(name?, domain?, q?, page, pageSize, sort?)`: `name` = case-insensitive partial match, `domain` = host/registrable-domain match, `q` = relevance search scored by `CompanySearchScorer` (Domain, pure: exact > prefix > token > fuzzy, over name + domain) and ordered by score desc. When `q` is absent, sort is `name|createdAt` asc/desc. An empty result returns 200 with `items: []`.
- [ ] `CompanyResponse(Guid Id, string Name, string WebsiteUrl, string Domain, DateTimeOffset CreatedAt)` plus optional `Relevance` on search results. A manual mapping extension, no AutoMapper.
- [ ] Unit tests: handlers with NSubstitute fakes, the validation behavior, scorer ordering.

## Phase 3 — Infrastructure  ☐
- [ ] `AppDbContext` (`internal`), `CompanyConfiguration` (value converters for `CompanyId`/`CompanyName`/`WebsiteUrl`, `RegistrableDomain` stored as a column so later SQL can index it).
- [ ] `CompanyRepository`: filters (`name`, `domain`) go through EF queries (`AsNoTracking`, `EF.Functions`-free so they're portable). Relevance `q` pre-filters candidates in the query, then scores in memory with the domain scorer. Document that a real DB would replace this with FTS/trigram behind the same interface.
- [ ] `HttpWebsiteContentProvider`: typed `HttpClient` + `Microsoft.Extensions.Http.Resilience` (timeout 3s, 1 retry). **SSRF guard**: resolve DNS, reject private/loopback/link-local IPs, http/https only, max 2 redirects, read ≤256 KB, HTML parsed with AngleSharp. Failures degrade to "not relevant" with a reason, never 500.
- [ ] `AddInfrastructure(IServiceCollection, IConfiguration)` with `InMemoryDatabase` name from config. This one method is the swap point for SQL.

## Phase 4 — API (FastEndpoints)  ☐
- [ ] `Program.cs`: Serilog, `AddFastEndpoints`, `AddMediator`, `AddApplication`, `AddInfrastructure`, `UseFastEndpoints(c => { c.Versioning.Prefix="v"; c.Endpoints.RoutePrefix="api"; c.Errors.UseProblemDetails(); })`, global exception handler → 500 ProblemDetails (logged), OpenAPI (FastEndpoints.Swagger / Scalar UI), CORS for `http://localhost:4200`, `launchSettings.json` on **http://localhost:5000** and **https://localhost:7000**.
- [ ] Endpoints (thin: bind request DTO → `mediator.Send` → map `ErrorOr` to a result):
  - `POST /api/v1/companies` → **201** + `Location: /api/v1/companies/{id}`, **400** ProblemDetails (`errors` dict + concise `detail`).
  - `GET /api/v1/companies/{id:guid}` → **200** / **404** ProblemDetails.
  - `GET /api/v1/companies?name=&domain=&q=&sort=&page=&pageSize=` → **200** `PagedResponse` (empty list when nothing matches), **400** on bad query params. Retrieving "all" is the same route with no filters.
- [ ] `ErrorOrExtensions.ToProblem()`: Validation→400, NotFound→404, Conflict→409, the rest→500.
- [ ] Extensibility notes in README: PUT/PATCH/DELETE just add one slice and one endpoint each. Paging and sorting already exist.
- [ ] Integration tests (`WebApplicationFactory` with a fake `IWebsiteContentProvider`): 201 + Location, each 400 rule, 404, search filters, relevance ordering, empty result, ProblemDetails shape.
- [ ] Architecture tests: Domain has no deps, Application has no EF/FastEndpoints refs, endpoints have no repository refs.

## Phase 5 — Frontend (Angular 21)  ☐
- [ ] `core`: `API_BASE_URL` InjectionToken from `environment` (dev: `http://localhost:5000/api/v1`), `provideHttpClient(withInterceptors([problemDetailsInterceptor]))` that normalizes errors into a typed `ApiError { title, detail, fieldErrors }`.
- [ ] `data-access/companies.api.ts`: `create()`, `getById()`, `search(criteria)` return `Observable`s. `company.models.ts` hand-written DTO types matching the API.
- [ ] `ui/company-form`: typed Reactive Form (`NonNullableFormBuilder`). Client-side mirrors of the required/min-length/URL-pattern rules give fast feedback, and server errors map onto fields plus a top-level message. `output()` submit.
- [ ] `ui/company-search-bar` (q / name / domain), `ui/company-table` (`@for` with an `@empty` state), `shared/ui/alert`.
- [ ] `feature/companies-page`: `criteria$` → `debounceTime(300)` → `distinctUntilChanged` → `switchMap(api.search)` with loading/error state; a refresh `Subject` fires after a successful create. Template consumes via `toSignal`; local flags (submitting, banner message) are `signal`s. OnPush everywhere.
- [ ] Minimal styling (plain CSS, maybe a classless base like Pico via CDN-free import if desired).
- [ ] Tests (Vitest + Testing Library): form validation and error rendering, page flow with `HttpTestingController`, interceptor mapping.

## Phase 6 — Docs & polish  ☐
- [ ] README: prerequisites (.NET 10 SDK, Node 22+, `corepack enable`), run commands (`dotnet run --project backend/src/Verus.Api`, `pnpm start`), curl examples, API table, **relevance strategy explanation**, design decisions and trade-offs (EF InMemory vs dictionary, Mediator vs FE command bus, no UoW, SSRF guard), extensibility notes.
- [ ] `backend/src/Verus.Api/Companies.http` request samples.
- [ ] Final `scripts/ci` green, then tag `v1.0.0`.

---

## Verification
1. `./scripts/ci.ps1` passes: locked restore, format check, warnings-as-errors build, all .NET tests, FE lint/format/test/build.
2. `git commit` with a bad message gets rejected (commitlint). A badly formatted file gets blocked by pre-commit.
3. `dotnet run --project backend/src/Verus.Api` → open Scalar UI, then via curl:
   - `POST {"name":"Microsoft","websiteUrl":"https://www.microsoft.com"}` → 201 + Location
   - `POST {"name":"Apple","websiteUrl":"https://www.microsoft.com"}` → 400 "Company name is not relevant to the website domain"
   - `POST {"name":"","websiteUrl":"nope"}` → 400 with both field errors
   - `GET /api/v1/companies/{random guid}` → 404; `GET ?q=micro` → ordered results; `GET ?name=zzz` → 200 `[]`
4. `pnpm start` in `/frontend`: submit valid and invalid companies, see the messages, list refreshes, search filters live.
5. `pnpm install` with npm/yarn is refused (`only-allow`). A csproj with `Version=` fails restore (CPM).
