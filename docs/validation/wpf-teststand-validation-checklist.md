# WPF Functional Test Station Validation Checklist

Date: 2026-06-30

## Purpose

Use this checklist before production use of the WPF rewrite. The stable WPF-owned TestPlan is the production execution source. The exported TestStand profile is historical reference only.

## Build And Offline Evidence

- WPF solution builds as x86.
- Unit tests pass.
- Stable TestPlan loads successfully: `src/RfpTestStation/Rfp7000V2.testplan.json`.
- TestPlan name, version, product, and item count are recorded by the WPF Mock run path.
- TestPlan hash is recorded in the report before production release.
- `profile.html` imports successfully only if historical comparison tooling is being used.
- Imported profile contains 12 sequences and 820 steps.
- `MainSequence` imports 671 steps.
- `MainSequence` section counts are 16 setup, 627 main, 28 cleanup.
- Adapter routing resolves real profile module paths for Modbus, DAQ, USB I2C, oscilloscope, signal generator, config, prepared artifact paths, and flashing scripts.
- Existing external flashing `bat` and `ps1` files remain script-driven.
- Automated Mock WPF ViewModel smoke executes the current migration path from `profile.html` and writes JSON/CSV reports under `Project/Reports`.
- UI Automation Mock smoke opens the WPF window, enters SN, clicks Start, completes the current mock workflow, and writes PASS JSON/CSV reports.
- Functional workflow layer is introduced before production validation, with source references back to imported TestStand steps where useful.

## Old TestStand Run Record

- Date/time:
- Operator:
- Station:
- Product:
- SN:
- TestStand sequence file:
- TestStand report path:
- Final result:
- First failing step, if any:
- External RFP log path:
- External RedCase log path:
- External TDDI log path:

## WPF Run Record

- Date/time: 2026-06-30 14:35:21
- Operator: UI Automation smoke
- Station: Development PC
- Product: Mock
- SN: UIe197a1cf118c4f4d8a473b6da958c3f5
- WPF executable path: `src/RfpTestStation/RfpTestStation.App/bin/x86/Debug/net48/RfpTestStation.App.exe`
- Execution mode: Mock
- WPF JSON report path: `Project/Reports/UIe197a1cf118c4f4d8a473b6da958c3f5_20260630_143521_Pass.json`
- WPF CSV report path: `Project/Reports/UIe197a1cf118c4f4d8a473b6da958c3f5_20260630_143521_Pass.csv`
- Final result: Pass
- First failing step, if any: None
- External RFP log path: N/A in Mock mode
- External RedCase log path: N/A in Mock mode
- External TDDI log path: N/A in Mock mode

## Functional Coverage Comparison

- Production-critical flashing steps are covered.
- Fixture preparation and safety checks are covered.
- FCT I2C checks are covered.
- Oscilloscope / PWM / backlight checks are covered.
- AC input checks are covered.
- Signal generator checks are covered.
- Result output and OK/NG behavior are covered.
- Cleanup actions are covered after pass, fail, stop, and error.
- Imported step coverage report reviewed.
- First difference:
- Difference classification: unintended functional difference / intentional WPF improvement / migration-only difference.
- Resolution:

## Result Comparison

- Final PASS/FAIL matches old TestStand run.
- Numeric limits match production intent.
- String value checks match production intent.
- Pass/fail checks match production intent.
- Failure handling reaches cleanup.
- Stop handling reaches cleanup where possible.
- OK/NG output behavior observed.

## Intentional WPF Differences

Record each intentional difference before production use.

| Area | TestStand Reference Behavior | WPF Functional Behavior | Reason | Validation Evidence | Approved By |
| --- | --- | --- | --- | --- | --- |
| Example | | | | | |

## Semi-Hardware Validation

Run one subsystem at a time on the fixture PC.

| Subsystem | Validation Step | Expected Result | Actual Result | Pass/Fail | Notes |
| --- | --- | --- | --- | --- | --- |
| Modbus IO | Read one input and write one output | Correct channel changes | | | |
| Flash RFP | Run existing RFP bat through WPF adapter | Existing log path and PASS/FAIL captured | | | |
| Flash RedCase | Run existing RedCase bat through WPF adapter | Existing log path and PASS/FAIL captured | | | |
| Flash TDDI | Run existing TDDI bat through WPF adapter | Existing log path and PASS/FAIL captured | | | |
| USB I2C | Write/read known command | Expected bytes returned | | | |
| DAQ | Read known voltage channel | Value within expected range | | | |
| Oscilloscope | Read Vavg or frequency | Value within expected range | | | |
| Signal generator | Configure known output state | Instrument state changes as expected | | | |

## Full-System Validation

- Run one known-good product through old TestStand.
- Run the same product through WPF Hardware mode.
- Compare final result.
- Compare first failure, if any.
- Compare flashing logs.
- Compare critical report values and limits.
- Confirm cleanup output states.
- Confirm operator can start, stop, and reset from WPF.
- Confirm WPF report files are stored in the agreed production location.

## Open Items

- LabVIEW source code is intentionally not reviewed yet.
- `PowerControl.vi` now routes to the direct C# `PowerSupplyAdapter` TCP replacement.
- `ReadSN.vi`, MES, and PLC LabVIEW steps are explicit placeholders until their direct interfaces are confirmed.
- In Hardware mode, remaining LabVIEW placeholders return `Error` with a `PLACEHOLDER` message. They must not silently pass.
- Manual fixture validation has not been run in this workspace.
- Functional workflow extraction has started. WPF Mock mode now loads the stable TestPlan path; `profile.html` and `.seq` are no longer intended runtime inputs.
- TestPlan item executor is wired through WPF. Mock mode supports all current items; Hardware mode supports Flash, fixture IO, safety, result output, cleanup, USB I2C measurement, oscilloscope measurement, DAQ voltage limit items, and PowerSupply TCP control. SerialNumber, MES, and PLC replacements remain explicit placeholders until their interfaces are confirmed.
- Stable TestPlan now includes data-driven `FunctionalCheck` items for confirmed HVAC SW/encoder 7-byte word-range checks, HVAC SW byte-sequence checks, HVAC IND I2C-write/DAQ-voltage checks, Button/WakeSP single-byte I2C checks, SW-Pack PWM checks, and HVAC backlight PWM checks.
