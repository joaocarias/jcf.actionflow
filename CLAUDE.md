# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

jcf-actionflow imports the JSON export of an IBM Watson Assistant "action" skill (OpenAPI-ish
workspace export), lets you inspect and edit the actions/steps/collections graph, and
re-exports a JSON that can be reimported into Watson without loss.

## Layout

```
src/
  backend/
    Jcf.ActionFlow.slnx
    Jcf.ActionFlow.Core/   # domain models, parser, graph builder, copy/move, validator (no ASP.NET)
    Jcf.ActionFlow.Api/    # minimal API endpoints, CORS, ProblemDetails, Scalar docs in dev
    Jcf.ActionFlow.Tests/  # xUnit, covers Core + one HTTP smoke test
    README.md              # curl walkthrough + design decisions, read this first for backend work
  frontend/
    jcf-actionflow-app/    # React + TS + Vite + Tailwind v4, react-router-dom
samples/
  Lab-Chat-action.json     # real Watson export, used as the golden test fixture
docker-compose.yml
```

## Backend (.NET 10)

```bash
cd src/backend
dotnet build                                    # whole solution
dotnet test                                     # whole solution
dotnet test --filter FullyQualifiedName~RoundTripTests   # single test class
dotnet run --project Jcf.ActionFlow.Api         # http://localhost:5000, /scalar for docs in dev
```

Tests use `samples/Lab-Chat-action.json` (repo root) as fixture, copied into the test
output dir by `Jcf.ActionFlow.Tests.csproj`. See `src/backend/README.md` for the full
curl workflow (upload → graph → copy → validate → export) and the design decisions behind
the domain model.

## Frontend (React + TypeScript + Vite)

```bash
cd src/frontend/jcf-actionflow-app
npm install
npm run dev       # Vite dev server, http://localhost:5173 by default
npm run build     # tsc -b && vite build
npm run lint       # oxlint
```

Routing is `react-router-dom` (`BrowserRouter`); `nginx.conf` in this folder has the SPA
`try_files` fallback needed for that in the Docker image — keep it if you touch the
Dockerfile.

## Docker

```bash
docker compose up --build        # both api (5000) and web (5173) — see .env for ports
docker compose up --build api    # backend only
```

`api`'s build context is `src/backend` (so `Jcf.ActionFlow.Core` is available to the
Dockerfile alongside `Jcf.ActionFlow.Api`). No database/volume — the backend is in-memory
only for now (see "design decisions" in `src/backend/README.md`).
