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
  src/Verus.Api               # Features/Companies/<UseCase>/{Endpoint,Request,Summary}.cs, ErrorOr→ProblemDetails mapping, Program.cs, launchSettings (5000/7000)
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

## Phase 0 — Project setup & tooling  ✅
**Repo**
- [x] `git init`, `main` branch, `.gitignore` (dotnet + node), `.gitattributes` (`* text=auto eol=lf`), root `.editorconfig` (C# rules + 2-space TS/JSON/HTML).
- [x] `AGENTS.md`: architecture, dependency rule, slice recipe ("how to add a feature"), commands (build/test/lint/format/ci), conventions (naming, ErrorOr, no business logic in endpoints, DTO-only contracts, Conventional Commits). `CLAUDE.md` just points to it.
- [x] `TODO.md` = this plan.

**Backend tooling and package management**
- [x] `global.json` pins SDK 10.0.x.
- [x] `Directory.Build.props`: `net10.0`, `Nullable=enable`, `ImplicitUsings`, `LangVersion=latest`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `GenerateDocumentationFile` (Api), `RestorePackagesWithLockFile=true`, `RestoreLockedMode` when `CI=true`, `NuGetAudit=true` / `NuGetAuditMode=all` / `NuGetAuditLevel=low`, warnings-as-errors for NU1901-NU1904.
- [x] `Directory.Packages.props`: `ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true`, and every version lives here (no `Version=` in csproj). Analyzers go in as `GlobalPackageReference`: Meziantou.Analyzer, SonarAnalyzer.CSharp.
- [x] `nuget.config`: `<clear/>` + nuget.org only + `packageSourceMapping` (`*` → nuget.org).
- [x] Local tool manifest: `csharpier` 1.3 (formatter, `dotnet csharpier check .` in CI).
- [x] Create `Verus.slnx` and the 4 src + 4 test projects, all wired with project references.

**Frontend tooling and package management**
- [x] `corepack enable`, then `pnpm dlx @angular/cli@21 new frontend --package-manager=pnpm --style=css --ssr=false --zoneless --routing`.
- [x] `package.json`: `"packageManager": "pnpm@<latest 10.x>"`, `"engines": { "node": ">=22.12 <23 || >=24", "pnpm": ">=10" }`, `"preinstall": "npx only-allow pnpm"`.
- [x] `.npmrc`: `engine-strict=true`, `save-exact=true`, `strict-peer-dependencies=true`, `auto-install-peers=false`.
- [x] `pnpm-workspace.yaml` settings: `minimumReleaseAge: 1440` (supply-chain delay), `onlyBuiltDependencies: [esbuild, lmdb, ...]` (explicit allow-list for install scripts). Commit `pnpm-lock.yaml`, and CI uses `pnpm install --frozen-lockfile`. Add `pnpm audit --prod` to CI.
- [x] `tsconfig`: Angular strict + `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`, `noImplicitOverride`, strict templates.
- [x] ESLint (`ng add angular-eslint`, flat config, typed rules, `@angular-eslint/template` accessibility rules, `prefer-on-push`, no `any`). Prettier + `eslint-config-prettier`.

**Local CI and hooks**
- [x] `scripts/ci.ps1` + `ci.sh`: backend `restore --locked-mode → csharpier check → build -warnaserror → test (coverage)`. Frontend `pnpm install --frozen-lockfile → lint → prettier --check → test --watch=false → build`.
- [x] `lefthook.yml`: **pre-commit** = csharpier + prettier + eslint on staged files (fast). **commit-msg** = commitlint (Conventional Commits). **pre-push** = full `scripts/ci`. Install Lefthook as a frontend devDependency plus `lefthook install` in a `prepare` script (the root `package.json` is a tiny pnpm root only if needed; otherwise run from `/frontend` with `--cwd ..`).
- [x] `.github/workflows/ci.yml` runs the same script on ubuntu (setup-dotnet 10, pnpm/action-setup, node 22).
- [x] ✅ Exit criterion: empty solution + fresh Angular app pass `scripts/ci` and the hooks fire.

## Phase 1 — Domain model (Okhravi / Horvat)  ✅
- [x] `CompanyId` (readonly record struct over Guid v7, `Guid.CreateVersion7()`).
- [x] `CompanyName` value object: trimmed, not empty, length 3–200 ("at least a few characters"). `static ErrorOr<CompanyName> Create(string)`.
- [x] `WebsiteUrl` value object: absolute `http/https` `Uri`, host has a dot, no userinfo, normalized (lowercase host). Exposes `Host` and `RegistrableDomain` (via `Nager.PublicSuffix`, e.g. `shop.example.co.uk` → `example.co.uk`).
- [x] `Company` aggregate: private ctor, `static ErrorOr<Company> Create(CompanyName, WebsiteUrl, RelevanceAssessment, TimeProvider)`. The aggregate refuses creation if the assessment isn't relevant, so there's no invalid Company. Has `CreatedAt`.
- [x] `DomainErrors.Company.*` (codes like `Company.Name.TooShort`, `Company.Website.Invalid`, `Company.NameNotRelevantToWebsite`) with concise, user-friendly messages.
- [x] **Relevance (pure, in Domain)**: `CompanyNameNormalizer` (lowercase, remove diacritics, strip legal suffixes inc/llc/ltd/corp/gmbh/sa/co, punctuation, `&`→and), plus `NameDomainMatcher` producing `RelevanceAssessment(Score 0..1, Method, Reason)`. Rules, best score wins:
  exact squashed match (`"Acme Corp"` ↔ `acme.com`) = 1.0; domain label contains all name tokens / name contains domain label = 0.9; acronym (`International Business Machines` ↔ `ibm.com`) = 0.85; Jaro-Winkler similarity of squashed strings (F23.StringSimilarity). Threshold `0.8`, configurable via `RelevanceOptions`.
- [x] Unit tests: table-driven `[Theory]` for each value object and matcher rule, plus negative cases.

## Phase 2 — Application layer (vertical slices)  ✅
- [x] Abstractions: `ICompanyRepository { AddAsync, GetByIdAsync, SearchAsync(CompanySearchCriteria) }` (returns domain objects, never EF types), `IWebsiteContentProvider { Task<WebsiteMetadata?> GetAsync(Uri, ct) }`, `IRelevanceEvaluator`.
- [x] `RelevanceEvaluator` (strategy chain): domain matcher, then on failure the content strategy (fetch title / `og:site_name` / `application-name`, then fuzzy match against those) → `RelevanceAssessment` with method `Domain` | `Content` and reason.
- [x] Shared: `PagedRequest(page=1,pageSize=20, max 100)`, `PagedResponse<T>(items,page,pageSize,totalCount)`.
- [x] Behaviors: `ValidationBehavior<TReq,TRes>` (FluentValidation → `ErrorOr` validation errors, no throwing) and `LoggingBehavior` (start/end/failure with request name + elapsed).
- [x] Slices (each folder = command/query + validator + handler + response DTO):
  - `CreateCompany` → `ErrorOr<CompanyResponse>`; logs created/rejected (with relevance reason).
  - `GetCompanyById` → `ErrorOr<CompanyResponse>` (`Error.NotFound`).
  - `SearchCompanies(name?, domain?, q?, page, pageSize, sort?)`: `name` = case-insensitive partial match, `domain` = host/registrable-domain match, `q` = relevance search scored by `CompanySearchScorer` (Domain, pure: exact > prefix > token > fuzzy, over name + domain) and ordered by score desc. When `q` is absent, sort is `name|createdAt` asc/desc. An empty result returns 200 with `items: []`.
- [x] `CompanyResponse(Guid Id, string Name, string WebsiteUrl, string Domain, DateTimeOffset CreatedAt)` plus optional `Relevance` on search results. A manual mapping extension, no AutoMapper.
- [x] Unit tests: handlers with NSubstitute fakes, the validation behavior, scorer ordering.

## Phase 3 — Infrastructure  ✅
- [x] `AppDbContext` (`internal`), `CompanyConfiguration` (value converters for `CompanyId`/`CompanyName`/`WebsiteUrl`, `RegistrableDomain` stored as a column so later SQL can index it).
- [x] `CompanyRepository`: filters (`name`, `domain`) go through EF queries (`AsNoTracking`, `EF.Functions`-free so they're portable). Relevance `q` pre-filters candidates in the query, then scores in memory with the domain scorer. Document that a real DB would replace this with FTS/trigram behind the same interface.
- [x] `HttpWebsiteContentProvider`: typed `HttpClient` + `Microsoft.Extensions.Http.Resilience` (timeout 3s, 1 retry). **SSRF guard**: resolve DNS, reject private/loopback/link-local IPs, http/https only, max 2 redirects, read ≤256 KB, HTML parsed with AngleSharp. Failures degrade to "not relevant" with a reason, never 500.
- [x] `AddInfrastructure(IServiceCollection, IConfiguration)` with `InMemoryDatabase` name from config. This one method is the swap point for SQL.

## Phase 4 — API (FastEndpoints)  ✅
- [x] `Program.cs`: Serilog, `AddFastEndpoints`, `AddMediator`, `AddApplication`, `AddInfrastructure`, `UseFastEndpoints(c => { c.Versioning.Prefix="v"; c.Endpoints.RoutePrefix="api"; c.Errors.UseProblemDetails(); })`, global exception handler → 500 ProblemDetails (logged), OpenAPI (FastEndpoints.Swagger / Scalar UI), CORS for `http://localhost:4200`, `launchSettings.json` on **http://localhost:5000** and **https://localhost:7000**.
- [x] Endpoints (thin: bind request DTO → `mediator.Send` → map `ErrorOr` to a result):
  - `POST /api/v1/companies` → **201** + `Location: /api/v1/companies/{id}`, **400** ProblemDetails (`errors` dict + concise `detail`).
  - `GET /api/v1/companies/{id:guid}` → **200** / **404** ProblemDetails.
  - `GET /api/v1/companies?name=&domain=&q=&sort=&page=&pageSize=` → **200** `PagedResponse` (empty list when nothing matches), **400** on bad query params. Retrieving "all" is the same route with no filters.
- [x] `ErrorOrExtensions.ToProblem()`: Validation→400, NotFound→404, Conflict→409, the rest→500.
- [x] Extensibility notes in README: PUT/PATCH/DELETE just add one slice and one endpoint each. Paging and sorting already exist.
- [x] Integration tests (`WebApplicationFactory` with a fake `IWebsiteContentProvider`): 201 + Location, each 400 rule, 404, search filters, relevance ordering, empty result, ProblemDetails shape.
- [x] Architecture tests: Domain has no deps, Application has no EF/FastEndpoints refs, endpoints have no repository refs.

## Phase 5 — Frontend (Angular 21)  ✅
- [x] `core`: `API_BASE_URL` InjectionToken from `environment` (dev: `http://localhost:5000/api/v1`), `provideHttpClient(withInterceptors([problemDetailsInterceptor]))` that normalizes errors into a typed `ApiError { title, detail, fieldErrors }`.
- [x] `data-access/companies.api.ts`: `create()`, `getById()`, `search(criteria)` return `Observable`s. `company.models.ts` hand-written DTO types matching the API.
- [x] `ui/company-form`: typed Reactive Form (`NonNullableFormBuilder`). Client-side mirrors of the required/min-length/URL-pattern rules give fast feedback, and server errors map onto fields plus a top-level message. `output()` submit.
- [x] `ui/company-search-bar` (q / name / domain), `ui/company-table` (`@for` with an `@empty` state), `shared/ui/alert`.
- [x] `feature/companies-page`: `criteria$` → `debounceTime(300)` → `distinctUntilChanged` → `switchMap(api.search)` with loading/error state; a refresh `Subject` fires after a successful create. Template consumes via `toSignal`; local flags (submitting, banner message) are `signal`s. OnPush everywhere.
- [x] Minimal styling: plain CSS with design tokens in `styles.css` (light and dark), no UI library.
- [x] Tests (Vitest + TestBed; Testing Library not added, to keep deps minimal): form validation and error rendering, page flow with `HttpTestingController`, interceptor mapping.

## Phase 6 — Docs & polish  ✅
- [x] README: prerequisites (.NET 10 SDK, Node 22+, `corepack enable`), run commands (`dotnet run --project backend/src/Verus.Api`, `pnpm start`), curl examples, API table, **relevance strategy explanation**, design decisions and trade-offs (EF InMemory vs dictionary, Mediator vs FE command bus, no UoW, SSRF guard), extensibility notes.
- [x] `backend/src/Verus.Api/Companies.http` request samples.
- [x] Final `scripts/ci` green, then tag `v1.0.0`.

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
- Phase 1: the Public Suffix List is an **embedded resource** in Domain (`Resources/public_suffix_list.dat`, parsed once, lazily, by `Internet/PublicSuffix.cs`), so registrable-domain parsing does no runtime I/O. Refresh it by re-downloading the file.
- Phase 1: `CompanySearchScorer`, `NameDomainMatcher` and `ContentMatcher` are pure Domain services (static), built on `NormalizedText` (diacritic folding, legal-suffix stripping, acronyms) and Jaro-Winkler (`TextSimilarity`).
- Phase 1: errors carry `metadata["field"]` (`ErrorMetadata.ForField`) so the API can build a ProblemDetails `errors` dictionary keyed by JSON field.
- Phase 2: input validation = FluentValidation validators that **delegate to the value objects** (`MustBeValueObject(CompanyName.Create)`), so the domain is the single source of truth and all field errors come back in one response. `ValidationBehavior` turns failures into `ErrorOr` validation errors (no exceptions). It builds `ErrorOr<T>.From(List<Error>)` via a cached delegate, not `dynamic`.
- Phase 2: an extra rule beyond the brief: creating a second company for an already-registered registrable domain returns **409 Conflict** (`Company.Website.Duplicate`).
- Phase 2: the search contract is `SearchCompaniesQuery(name, domain, q, sort, page, pageSize)`. Without `q`, filter/sort/page go to the repository (DB-friendly `ListAsync`). With `q`, the handler ranks `ListAllAsync` candidates with `CompanySearchScorer` (default sort = relevance). `sort` accepts `relevance|name|-name|createdAt|-createdAt`.
- Phase 2: Mediator pipeline behaviors must be listed in the **Api's** `AddMediator(o => o.PipelineBehaviors = [...])` (the source generator reads them there): `LoggingBehavior<,>` first, then `ValidationBehavior<,>`.
- Phase 3: value objects map as EF **owned types** (columns `Name`, `WebsiteUrl`, `WebsiteHost`, `WebsiteDomain`, `WebsiteDomainLabel`), bound through their private constructors. `Microsoft.EntityFrameworkCore.Relational` is referenced only for table/column/index metadata. The unique index on `WebsiteDomain` is ignored by InMemory, so the handler checks for duplicates too.
- Phase 3: the SSRF guard lives in `SocketsHttpHandler.ConnectCallback` (`PublicAddressGuard`). It validates **every resolved IP at connect time**, which covers redirects and DNS rebinding. Its refusals (`UnsafeAddressException`) are excluded from Polly retries. Real-world caveat: many big sites (e.g. abc.xyz) return 403 to bots, so the content fallback degrades to "could not be inspected".
- Phase 4: errors returned from endpoints use `ApiProblem` (an `IResult` wrapping `FastEndpoints.ProblemDetails`) so they are served as `application/problem+json` in the **same shape** as FastEndpoints' own binding errors: `{type,title,status,detail,instance,traceId,errors:[{name,reason,code}]}`. Clients handle one format.
- Phase 4: `GET /companies/{id}` has no route constraint on purpose: a malformed id gives 400 and an unknown id gives 404. Malformed JSON gives 400 "The request body is not valid JSON." Nulls are omitted from responses.
- Phase 4: Serilog uses `preserveStaticLogger: true` so multiple `WebApplicationFactory` hosts can run in one test process.
- Phase 4: docs live at `/scalar/v1` (OpenAPI JSON at `/swagger/v1/swagger.json`). `/` redirects there.
- Phase 5: dev uses a proxy (`frontend/proxy.conf.json`, `/api` → :5000) and `API_BASE_URL` defaults to `/api/v1`, so no environment files are needed.
- Phase 5: there is also a company **detail page** (`/companies/:id`, via `withComponentInputBinding`) that exercises `GET /companies/{id}`, including its 404 state.
- Phase 5: `CompaniesPage` streams: an immediate first load, debounced filter changes, a filter change resets to page 1, `exhaustMap` blocks double submits, and a successful create re-runs the current search. `linkedSignal` keeps the last page visible while the next one loads.
- Phase 5: ESLint turns `@typescript-eslint/unbound-method` off (Angular `Validators.*` are meant to be passed unbound), and `template/no-call-expression` is not used because signals are function calls.
- Phase 6: verified end to end via the dev-server proxy with curl. The UI itself was **not** visually checked in a browser in this session (the browser automation tool was unavailable).

## Possible follow-ups (not required by the brief)
- Visual/E2E smoke test (Playwright) of the Angular page.
- Upgrade pnpm 10.27 → latest major. Refresh the embedded Public Suffix List periodically.
- Curated alias / registry strategy for brands whose domain shares nothing with the name (e.g. Alphabet ↔ abc.xyz behind a bot wall).
- Generate the frontend DTO types from the OpenAPI document (`/swagger/v1/swagger.json`) instead of hand-writing them.
