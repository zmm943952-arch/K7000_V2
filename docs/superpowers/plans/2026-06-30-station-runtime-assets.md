# Station Runtime Assets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Copy the required flashing scripts, runtime tools, firmware folders, and hardware DLLs from the legacy `Project` tree into a new `src/RfpTestStation/StationRuntime` tree and update the WPF rewrite to use that organized runtime tree.

**Architecture:** Keep legacy files untouched. Introduce `StationRuntime` as the new runtime asset root under the WPF solution, update defaults/configuration to point there, and update adapter assembly hint paths to use copied DLLs.

**Tech Stack:** PowerShell file copy, .NET Framework 4.8 project references, JSON config, xUnit.

---

## Tasks

### Task 1: Default Config Path

- [ ] Write a failing `MainViewModelTests` assertion that default `ConfigJsonPath` is `src/RfpTestStation/StationRuntime/Config/Config.json`.
- [ ] Update `MainViewModel` default path.
- [ ] Run `MainViewModelTests`.

### Task 2: Copy Runtime Assets

- [ ] Create `src/RfpTestStation/StationRuntime/Config`.
- [ ] Create `src/RfpTestStation/StationRuntime/Flash/RFP_Auto`.
- [ ] Create `src/RfpTestStation/StationRuntime/Flash/RedCase_Auto`.
- [ ] Create `src/RfpTestStation/StationRuntime/Flash/TDDI_Auto`.
- [ ] Create `src/RfpTestStation/StationRuntime/Lib/Hardware`.
- [ ] Copy required files only; do not copy `.vs`, `obj`, `Backup`, logs, prepared marker txt files, pdb files, or vshost files.

### Task 3: Update Runtime Config

- [ ] Update copied `StationRuntime/Config/Config.json` script and firmware local paths to point inside `StationRuntime/Flash`.
- [ ] Update `Rfp7000V2.testplan.json` flash script paths to point inside `StationRuntime/Flash`.

### Task 4: Update Project References

- [ ] Update `RfpTestStation.Adapters.csproj` HintPath entries to use `StationRuntime/Lib/Hardware`.
- [ ] Build the solution.

### Task 5: Verify

- [ ] Run full tests.
- [ ] Build x86 app.
- [ ] Verify required runtime files exist.
