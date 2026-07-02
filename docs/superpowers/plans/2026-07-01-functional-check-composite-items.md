# Functional Check Composite Items Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add TestPlan composite items that express the confirmed TestStand FCT patterns as data-driven checks.

**Architecture:** Extend `TestItemKind` with `FunctionalCheck` and execute templates inside `TestPlanItemExecutor`. Keep low-level hardware access through the existing Modbus, PowerSupply, UsbI2c, DaqVoltage, and Oscilloscope adapters.

**Tech Stack:** C#/.NET Framework 4.8, xUnit, Newtonsoft.Json `JObject` parameters, existing hardware adapter abstractions.

---

### Task 1: FunctionalCheck Routing

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.Core/Workflow/TestItemKind.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Adapters/TestPlans/TestPlanItemExecutor.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/TestPlans/TestPlanItemExecutorTests.cs`

- [x] Add `FunctionalCheck` to `TestItemKind`.
- [x] Add hardware execution routing for `FunctionalCheck`.
- [x] Add mock-mode value handling for `FunctionalCheck`.
- [x] Verify existing tests still pass.

### Task 2: I2C Byte Sequence Check Template

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.Adapters/TestPlans/TestPlanItemExecutor.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/TestPlans/TestPlanItemExecutorTests.cs`

- [x] Write a failing test for an HVAC SW style check: relay on, power off CH3, read `0xD4`, compare first bytes, power on CH3, compare high bytes, relay off.
- [x] Implement `template = I2cByteSequence`.
- [x] Parse expected byte strings such as `"88 88 08"`.
- [x] Return `Failed` when actual bytes do not match.

### Task 3: I2C Write + DAQ Voltage Template

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.Adapters/TestPlans/TestPlanItemExecutor.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/TestPlans/TestPlanItemExecutorTests.cs`

- [x] Write a failing test for an HVAC IND style check: write high data to I2C, wait, read DAQ voltage in high limits, write low data, read DAQ voltage in low limits.
- [x] Implement `template = I2cWriteDaqVoltage`.
- [x] Preserve the actual command in the result message for report traceability.
- [x] Return `Failed` when any limit fails.

### Task 4: Stable TestPlan Data

**Files:**
- Modify: `src/RfpTestStation/Rfp7000V2.testplan.json`
- Test: `src/RfpTestStation/RfpTestStation.Tests/TestPlans/TestPlanRepositoryTests.cs`

- [x] Add `I2cWriteReadWordRange` for HVAC SW/encoder 7-byte checks that evaluate `(read[5] << 8) + read[4]`.
- [x] Add confirmed functional check items for TMP1_PHSA, TMP1_PHSB, TMP2_PHSA, TMP2_PHSB, FAN_UP_SW, and FAN_DN_SW.
- [x] Add confirmed functional check items for HVAC SW and HVAC IND.
- [x] Add confirmed functional check items for Button S1-S6 and WakeSP.
- [x] Add confirmed functional check items for SW-Pack PWM1-PWM4.
- [x] Add confirmed functional check items for HVAC backlight PWM outputs.
- [x] Add representative smoke coverage that the plan loads and includes the new item ids.
- [x] Keep full signal lists data-driven and avoid duplicating low-level TestStand report-only steps.

### Task 5: Verification

- [x] Run focused TestPlan executor tests.
- [x] Run the full test project.
- [x] Update validation notes if the stable TestPlan coverage changes.
