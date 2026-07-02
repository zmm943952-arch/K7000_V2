# Production Readiness Gates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the first production-readiness gates: block runs with missing/invalid SN, run a lightweight file/config preflight before execution, and capture external flash script log paths in the main run log.

**Architecture:** Keep the behavior close to existing app/reporting boundaries. Add small Core services for SN validation and preflight checks, extend process results with extracted external log paths, and have `MainViewModel` write those details into `RunReport`/`RunLogWriter`.

**Tech Stack:** C# .NET Framework 4.8, WPF MVVM, xUnit.

---

### Task 1: SN Validation Gate

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Validation/SerialNumberValidator.cs`
- Modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/App/MainViewModelTests.cs`

- [ ] Write a failing test that `StartRunAsync` blocks when `SerialNumber` is empty and logs a clear message.
- [ ] Implement `SerialNumberValidator` and call it before loading the plan/config.
- [ ] Verify the test passes.

### Task 2: Preflight Checks

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Preflight/StationPreflightChecker.cs`
- Modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/App/MainViewModelTests.cs`

- [ ] Write a failing test that missing configured flash script paths block the run before workflow execution.
- [ ] Implement checks for test plan file, config file, configured flash scripts, and TCON bin when configured.
- [ ] Log every preflight result so the run log/UI shows what passed or failed.

### Task 3: External Script Log Path Capture

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.Core/Abstractions/IExternalProcessRunner.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Adapters/Flashing/FlashAdapter.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Core/Model/StepResult.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Core/Reporting/RunLogWriter.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/Adapters/FlashAdapterTests.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/Reporting/ResultWriterTests.cs`

- [ ] Write a failing test where process stdout contains `Log saved to: C:\x\step.log` and the flash result exposes that path.
- [ ] Add `ExternalLogPath` to process/step results and parse common log output lines.
- [ ] Write `ExternalLogPath` in the run log for the matching step.

### Task 4: Verification

- [ ] Run `dotnet test .\src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj`.
- [ ] Run `dotnet build .\src\RfpTestStation\RfpTestStation.App\RfpTestStation.App.csproj -p:Platform=x86`.
