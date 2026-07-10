# Firmware Cache Delete Safety Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent RFP and RedCase firmware preparation from recursively deleting any directory not explicitly owned as a disposable firmware cache.

**Architecture:** Each PowerShell script validates its module-specific fixed cache boundary before both the fast path and cleanup. A marker file establishes ownership for non-empty directories, while lexical containment, root checks, and complete reparse-point checks prevent path escape and link traversal. Integration tests execute the real scripts against temporary deployment layouts.

**Tech Stack:** PowerShell 5.1, C#/.NET Framework 4.8, xUnit

---

### Task 1: Add failing RFP safety tests

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.Tests/Scripts/FlashPreparationScriptTests.cs`

- [ ] Add helpers that copy `Prepare_RfpFirmware.ps1` into a temporary `Flash/RFP_Auto/Scripts` layout and create the minimum config/project/MES fixtures.
- [ ] Add a test proving an outside-root directory containing a sentinel is rejected and unchanged.
- [ ] Add a test proving a non-empty unmarked directory below `Firmware` is rejected and unchanged.
- [ ] Add tests for root equality and sibling-prefix rejection.
- [ ] Add drive-root, config-directory, and MES-directory rejection cases where applicable.
- [ ] Add tests proving missing/empty targets gain `.rfp-firmware-cache`, and marked targets can be cleaned while retaining the marker.
- [ ] Add an already-current but unmarked target case proving the fast path still returns a nonzero exit.
- [ ] On Windows systems where junction creation succeeds, add tests for a junction in the target chain and a nested junction in the deletion subtree; skip only when junction creation is unavailable.
- [ ] Add an explicit allowed-root reparse-point rejection case.
- [ ] For every rejection, assert a nonzero exit with a safety/ownership error, sentinel preservation, and absence of newly copied firmware.
- [ ] Run `dotnet test RfpTestStation.Tests/RfpTestStation.Tests.csproj --no-restore --filter FullyQualifiedName~FlashPreparationScriptTests` from `src/RfpTestStation` and verify the new tests fail because the script currently accepts unsafe/unowned paths.

### Task 2: Implement RFP cache ownership validation

**Files:**
- Modify: `Runtime/Flash/RFP_Auto/Scripts/Prepare_RfpFirmware.ps1`
- Modify: `src/RfpTestStation/StationRuntime/Flash/RFP_Auto/Scripts/Prepare_RfpFirmware.ps1`

- [ ] Add constants/helpers for `.rfp-firmware-cache`, strict case-insensitive separator-boundary containment, drive/config/MES rejection, and reparse-point detection across the root-to-target chain and target subtree.
- [ ] Derive the allowed root as the `Firmware` sibling under the script's `RFP_Auto` directory; reject a reparse-point root and reject target equality with it.
- [ ] Validate the target before `Test-RfpPreparationCurrent` so the fast path cannot bypass safety.
- [ ] For missing/empty targets, create and mark them; reject non-empty unmarked targets.
- [ ] Replace cleanup with deletion of children except the ownership marker, after full subtree validation.
- [ ] Copy the completed script to the StationRuntime mirror and verify both files are byte-identical.
- [ ] Re-run the filtered tests and verify all RFP safety tests pass.

### Task 3: Add failing RedCase safety tests

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.Tests/Scripts/FlashPreparationScriptTests.cs`

- [ ] Add helpers that copy the RedCase script into a temporary `Flash/RedCase_Auto/Debug` layout.
- [ ] Add tests proving only the exact `Debug/Firmware_Local` path is accepted; outside and sibling paths with sentinels are rejected unchanged.
- [ ] Add a non-empty unmarked exact `Firmware_Local` case proving ownership is required and its sentinel remains.
- [ ] Add tests proving missing/empty fixed targets gain the marker and marked non-empty targets can be cleaned while retaining it.
- [ ] Add config-directory and MES-directory collision cases, including when either resolves to the otherwise-valid fixed cache.
- [ ] Add an already-current but unmarked fixed target case proving the fast path still returns a nonzero exit.
- [ ] Add available fixed-root/target reparse and nested-junction rejection tests for RedCase.
- [ ] For every rejection, assert a nonzero exit with a safety/ownership error, sentinel preservation, and absence of newly copied firmware.
- [ ] Run the filtered test command and verify the new RedCase tests fail for the expected missing validation.

### Task 4: Implement RedCase fixed-cache validation

**Files:**
- Modify: `Runtime/Flash/RedCase_Auto/Debug/Prepare_RedCaseFirmware.ps1`
- Modify: `src/RfpTestStation/StationRuntime/Flash/RedCase_Auto/Debug/Prepare_RedCaseFirmware.ps1`

- [ ] Add the same ownership, drive/config/MES, and reparse-point primitives used by RFP, adapted to require exact case-insensitive equality with `Debug/Firmware_Local`; config/MES equality remains forbidden.
- [ ] Validate and establish ownership before `Test-FileCurrent` so the fast path cannot bypass safety.
- [ ] Preserve the marker during cleanup and reject every other target.
- [ ] Copy the completed script to the StationRuntime mirror and verify both files are byte-identical.
- [ ] Re-run the filtered tests and verify all script tests pass.

### Task 5: Verify regression safety

**Files:**
- Modify if needed: `src/RfpTestStation/RfpTestStation.Tests/Scripts/FlashPreparationScriptTests.cs`

- [ ] Add an explicit test asserting each Runtime preparation script is byte-identical to its StationRuntime counterpart.
- [ ] Run `dotnet test RfpTestStation.sln --no-restore` from `src/RfpTestStation`.
- [ ] Confirm all tests pass with zero failures and inspect `git diff --check`.
- [ ] Review `git diff` to ensure no unrelated user changes were modified.
