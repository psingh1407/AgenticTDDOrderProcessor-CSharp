// ***************************************************************************
// Copyright (c) 2026, Industrial Logic, Inc., All Rights Reserved.
//
// This code is the exclusive property of Industrial Logic, Inc. It may ONLY be
// used by students during Industrial Logic's workshops or by individuals
// who are being coached by Industrial Logic on a project.
//
// This code may NOT be copied or used for any other purpose without the prior
// written consent of Industrial Logic, Inc.
// ****************************************************************************

import { defineConfig } from '@playwright/test';

// Outer-loop acceptance tests. Playwright boots the C# app itself (webServer below) and drives the
// real browser UI. These specs live under tests/*.e2e.ts — the harness's `acceptanceTests` surface:
// the agent may author them, but they run OUTSIDE the unit TDD cycle (via `npm run e2e`, not the shim).
export default defineConfig({
  testDir: './tests',
  testMatch: '**/*.e2e.ts',
  timeout: 30000,
  use: {
    baseURL: 'http://localhost:5188',
    headless: true,
  },
  webServer: {
    command: 'dotnet run --project OrderProcessor --urls http://localhost:5188',
    port: 5188,
    reuseExistingServer: false,
  },
});
