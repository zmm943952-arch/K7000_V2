# Functional Group Testplan Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce K7000 V2 functional testplan repetition by grouping repeated relay/power/check sequences while preserving per-subcheck failure reasons.

**Architecture:** Add one grouped functional-check template in `TestPlanItemExecutor` that executes a shared power setup once, then iterates child checks with their own relay/channel/write/measurement behavior. Keep existing templates intact for compatibility, and migrate the source testplan to grouped items in controlled functional areas.

**Tech Stack:** C# .NET Framework 4.8, WPF MVVM, Newtonsoft.Json testplan parameters, xUnit.

---

### Task 1: Add Group Executor Coverage

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.Tests/TestPlans/TestPlanItemExecutorTests.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Adapters/TestPlans/TestPlanItemExecutor.cs`

- [x] Write failing tests for `I2cFunctionalGroup` with repeated child checks.
- [x] Assert common `powerOnBefore` runs once for the group.
- [x] Assert each child reports a specific failure reason when its measurement fails.
- [x] Implement the minimum grouped dispatcher in `TestPlanItemExecutor`.
- [x] Run the targeted test file.

### Task 2: Migrate Testplan To Functional Groups

**Files:**
- Modify: `src/RfpTestStation/Rfp7000V2.testplan.json`
- Modify: `Runtime/TestPlans/Rfp7000V2.testplan.json`

- [x] Replace repeated FunctionalCheck runs with grouped items for position, HVAC switches, indicators, buttons, PWM, and backlight.
- [x] Keep existing flash, safety, fixture, measurement, limit, result, and cleanup steps unchanged.
- [x] Keep child names and limits explicit so failure messages still point at the exact hardware item.
- [x] Load testplan via existing repository tests.

### Task 3: Documentation And Verification

**Files:**
- Modify: `README.md`

- [x] Document the testplan grouping update and why it reduces cycle time.
- [x] Run `dotnet test src\RfpTestStation\RfpTestStation.sln --nologo`.
- [x] Commit and push to `origin/main`.
