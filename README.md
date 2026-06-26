# Order Lifecycle — C# (TDD with tdd-kitt)

A web-based order processing system you build **test-driven**, story by story, under the
tdd-kitt harness. The harness enforces the red → green → refactor cycle as you and your
agent work.

## What's here (the starter)

```
OrderProcessor/            the ASP.NET web app — you build the domain, REST API, here
  Program.cs               a runnable web host shell (serves /frontend, no routes yet)
OrderProcessor.Tests/      unit + in-process API tests (the fast inner loop)
  SmokeTest.cs             a seed test so the suite starts green
frontend/                  the static UI you build (index.html / app.js / styles.css)
tests/                     Playwright acceptance specs (*.e2e.ts) — the outer loop
package.json               Playwright, pre-declared
playwright.config.ts       boots the C# app and drives the browser
.tdd-kitt-harness.json     harness config (surfaces, test command, acceptance specs)
```

The starter deliberately ships **no** domain model, routes, or UI implementation — you
test-drive those. It does ship the toolchain, the web host shell, and a seed test.

## One-time setup

```
npm install                                      # installs Playwright (declared in package.json)
dotnet restore                                   # restores .NET dependencies
npx tdd-kitt init . csharp --agent copilot       # wires the gate hooks + captures the green baseline
# then restart your agent so it picks up the hooks
```

Playwright's browsers are downloaded once by `npm install` if not already cached.

## The two loops

- **Inner loop (gated TDD):** unit tests and in-process API tests (`WebApplicationFactory<Program>`),
  run through the harness shim (`tdd-kitt run test`). This is where red → green → refactor lives.
- **Outer loop (acceptance):** Playwright specs in `tests/*.e2e.ts`, run with `npm run e2e`.
  The agent may write these; they run *outside* the TDD cycle (they don't settle red/green).

## Running tests

- Inner loop: `tdd-kitt run test` (the agent uses this; never call `dotnet test` directly).
- Outer loop: `npm run e2e` (boots the app, drives the browser).
