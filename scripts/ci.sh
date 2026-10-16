#!/usr/bin/env bash
# Local CI: the same checks GitHub Actions runs. Usage: scripts/ci.sh [backend|frontend]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TARGET="${1:-all}"
export CI="${CI:-true}" # enables NuGet locked-mode restore (see Directory.Build.props)

step() { printf '\n\033[1;36m==> %s\033[0m\n' "$1"; }

backend() {
  cd "$ROOT/backend"
  step "backend: restore (locked)"
  dotnet tool restore
  dotnet restore --locked-mode
  step "backend: format check (csharpier)"
  dotnet csharpier check .
  step "backend: build (warnings as errors)"
  dotnet build --no-restore -c Release
  step "backend: test"
  dotnet test --no-build -c Release
}

frontend() {
  cd "$ROOT"
  step "frontend: install (frozen lockfile)"
  pnpm install --frozen-lockfile
  step "frontend: audit"
  pnpm audit --prod
  cd "$ROOT/frontend"
  step "frontend: lint"
  pnpm lint
  step "frontend: format check"
  pnpm format:check
  step "frontend: test"
  pnpm test
  step "frontend: build"
  pnpm build
}

case "$TARGET" in
  backend) backend ;;
  frontend) frontend ;;
  all) backend; frontend ;;
  *) echo "unknown target: $TARGET" >&2; exit 2 ;;
esac

step "CI passed"
