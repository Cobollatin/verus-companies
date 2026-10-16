# AGENTS.md

Guidance for AI agents and humans working in this repo. Read this first, then `TODO.md` (the live plan).

## What this is
Company API (.NET 10, FastEndpoints) + Angular 21 frontend. Spec: `Coding Exercise Angular .Net.pdf`.
Companies (name + website URL) are validated — including a name↔URL **relevance** check — stored in memory
(EF Core InMemory) and queried (all, by id, filter by name/domain, relevance-ranked search).

## Layout
```
backend/   Verus.slnx — src/{Domain,Application,Infrastructure,Api}, tests/*
frontend/  Angular 21 app (pnpm workspace member)
scripts/   ci.sh / ci.ps1 — the local CI (also run by the pre-push hook and GitHub Actions)
```

## Commands
| Task | Command |
|---|---|
| Full local CI | `pnpm ci` (or `bash scripts/ci.sh`, `./scripts/ci.ps1`) — `backend`/`frontend` arg runs one side |
| Run API | `cd backend/src/Verus.Api && dotnet run` → http://localhost:5000, https://localhost:7000, docs at `/scalar` |
| Run FE | `cd frontend && pnpm start` → http://localhost:4200 |
| BE tests | `cd backend && dotnet test` |
| BE format | `cd backend && dotnet csharpier format .` |
| FE lint / format / test | `cd frontend && pnpm lint` / `pnpm format` / `pnpm test` |

## Backend architecture (Clean Architecture + vertical slices)
Dependency rule (enforced by `tests/Verus.ArchitectureTests`):
`Api → Application → Domain`, `Infrastructure → Application`. Api references Infrastructure **only** for DI composition.

- **Domain** — `Company` aggregate, value objects (`CompanyName`, `WebsiteUrl`, `CompanyId`), domain errors,
  pure relevance scoring. No I/O, no framework deps except `ErrorOr`. Illegal states are unrepresentable:
  construct via `static ErrorOr<T> Create(...)`, never public constructors.
- **Application** — `Features/<Aggregate>/<UseCase>/` slices: command/query record, FluentValidation validator,
  Mediator handler, response DTO. Ports (`ICompanyRepository`, `IWebsiteContentProvider`) live in `Abstractions/`.
  Cross-cutting concerns are Mediator pipeline behaviors (`Behaviors/`): validation, logging.
- **Infrastructure** — EF Core `AppDbContext` (internal), repositories, HTTP content provider. `AddInfrastructure`
  is the only swap point when replacing InMemory with a real database.
- **Api** — FastEndpoints endpoints: bind DTO → `mediator.Send` → map `ErrorOr` to HTTP. **No business logic.**
  Errors are RFC 9457 ProblemDetails. Routes: `/api/v1/...`.

### Adding a backend use case (recipe)
1. `Application/Features/Companies/<UseCase>/` — `<UseCase>Command|Query.cs` (record implementing `ICommand<ErrorOr<T>>`/`IQuery<...>`),
   `<UseCase>Validator.cs`, `<UseCase>Handler.cs`.
2. Extend the repository port only if needed; implement it in Infrastructure.
3. `Api/Endpoints/Companies/<UseCase>Endpoint.cs` — thin, returns `TypedResults`, maps errors via `ToProblem()`.
4. Tests: handler unit test + endpoint integration test.

### Conventions
- Expected failures → `ErrorOr` errors (`Validation`, `NotFound`, `Conflict`). Exceptions only for bugs/infrastructure.
- External contracts are DTO records. Never return domain entities or EF types from endpoints.
- Log key operations with `LoggerMessage` source-generated methods.
- `TreatWarningsAsErrors` is on; fix analyzer warnings, don't suppress them without a comment explaining why.
- Package versions live **only** in `backend/Directory.Packages.props` (Central Package Management). Lock files
  (`packages.lock.json`) are committed; CI restores in locked mode.

## Frontend architecture
```
src/app/core/                     app-wide singletons: config tokens, HTTP interceptors, error model
src/app/shared/ui/                dumb, reusable presentational components
src/app/features/<feature>/
  data-access/                    API services (HttpClient → Observable) + DTO types
  ui/                             presentational components (input()/output() only, no services)
  feature/                        smart/container components (wire data-access + ui)
```
- Standalone components only, `ChangeDetectionStrategy.OnPush` (lint-enforced), zoneless.
- RxJS for async/data streams (search: `debounceTime` → `distinctUntilChanged` → `switchMap`);
  signals for local UI state; bridge with `toSignal` at the template edge.
- Typed reactive forms (`NonNullableFormBuilder`). Server ProblemDetails errors map onto form fields.
- Built-in control flow (`@if`, `@for`) only. No `any`.

## Package management (pnpm, enforced)
- pnpm only (`only-allow` preinstall, `packageManager` field). Use `corepack enable`.
- Exact versions (`save-exact`), strict peers, `engine-strict`, `minimumReleaseAge: 1440` (no packages < 24h old),
  install scripts allow-listed in `pnpm-workspace.yaml`. Always commit `pnpm-lock.yaml`.
- Add deps with `pnpm --filter verus-frontend add <pkg>` (or `pnpm add` inside `frontend/`).

## Git
- Conventional Commits (commitlint, `commit-msg` hook). `pre-commit`: csharpier/prettier/eslint on staged files.
  `pre-push`: full local CI. Never bypass hooks with `--no-verify`.
- Commit after each completed `TODO.md` phase and tick its checkboxes.
