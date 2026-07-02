# WPF Functional Test Station Rewrite Design

Date: 2026-06-30

## Goal

Rewrite the existing TestStand/LabVIEW production test application as a WPF production test station. The new application must not depend on the TestStand execution engine or LabVIEW VI execution at runtime.

The existing TestStand sequence is a historical reference, not an execution source and not an editing source. The final target is functional equivalence: the WPF application must perform the same production testing, flashing, measurement, judgment, safety cleanup, and reporting work through clearer C# services.

Production execution must use a stable WPF-owned test plan file. `profile.html` and `.seq` are retained only for historical lookup and old/new comparison when needed.

External flashing tools and existing `bat`/`ps1` scripts remain in place and are not rewritten in C#.

## Runtime Inputs

The WPF rewrite uses these assets at runtime:

- `src/RfpTestStation/Rfp7000V2.testplan.json`
- `Project/Config.json`
- Existing flashing scripts under `RFP_Auto`, `RedCase_Auto`, and `TDDI_Auto`
- Existing callable .NET DLLs where appropriate

The following TestStand assets are historical references only:

- `Project/Teststand/RFP Auto Test Sequence/profile.html`
- `Project/Teststand/RFP Auto Test Sequence/RFP Auto Test Sequence.seq`

Manual IO point interpretation is not required for the first version. The stable test plan can use channel numbers directly, such as `ReadInputChannel(4)` and `WriteOutputChannel(2, True)`, unless a clearer adapter-level mapping is needed for maintainability.

## Functional Equivalence Rules

The rewrite does not copy every TestStand setting mechanically. The stable test plan is the only production flow source. The WPF implementation should use better boundaries, safer error handling, clearer adapter dispatch, and improved UI workflows where they make the test station easier to maintain and validate.

Rules:

- Production-critical test coverage, order dependencies, hardware side effects, flashing side effects, limits, pass/fail behavior, and cleanup behavior are defined by the WPF-owned test plan.
- `profile.html` and `.seq` must not be read during normal production execution.
- WPF code may replace TestStand mechanics such as callbacks, `Step.Result.*` assignments, `New Thread`, expression quirks, and flow-control implementation details with clearer C# equivalents.
- Validation compares functional outcomes and critical evidence, not one-to-one internal TestStand step behavior unless that comparison is useful for migration coverage.
- Where WPF intentionally differs from TestStand mechanics, the difference is documented and validated against the product test intent.
- Channel numbers and boolean/numeric values are imported from the sequence expressions by default.
- No separate IO point table is introduced for the first version.
- No Excel recipe or Excel configuration feature is introduced for the first version.
- Existing flashing scripts and external flashing tools stay script-driven.

## Non-Goals

The first rewrite does not attempt to:

- Rebuild TestStand as a general-purpose sequence editor.
- Execute LabVIEW VIs through a LabVIEW runtime.
- Rewrite Renesas RFP, RedCase, or TDDI flashing logic in C#.
- Replace production hardware protocols that already have callable .NET DLLs.
- Add a manual recipe system that does not exist in the current confirmed workflow.

## Existing Flow Summary

The exported profile contains:

- 12 sequences
- 820 total steps
- `MainSequence` with 671 steps
- `MainSequence` structure: 16 setup steps, 627 main steps, 28 cleanup steps

The high-level execution flow is:

1. Read configuration from `Config.json`.
2. Initialize Modbus IO, DAQ, oscilloscope, and related clients.
3. Clear relay outputs.
4. Power on and run fixture safety checks.
5. Run flashing steps:
   - MCU simple RFP flash
   - TCON / RedCase flash
   - TDDI flash
   - MCU shipping RFP flash
6. Wait for cylinder position.
7. Run FCT steps:
   - I2C write/read checks
   - button and switch checks
   - PWM / backlight oscilloscope measurements
   - AC input voltage checks
   - multiple numeric limit checks
   - signal generator checks
8. Power off and set OK/NG outputs based on result.
9. Run cleanup and close resources.

## Chosen Approach

Build a functional WPF test workflow, with the TestStand export used as a migration reference.

The application can import `profile.html` into a structured model during migration, but the production design should be organized around test station concepts: workflow orchestration, test items, hardware adapters, flashing services, safety cleanup, manual debug, and reporting. This keeps the new WPF application aligned with the proven production intent while allowing LabVIEW/TestStand runtime dependencies and awkward TestStand-specific mechanics to be removed.

Alternative approaches were rejected:

- Hardcoding the sequence directly in C# would be fast initially but difficult to maintain.
- Blindly cloning all TestStand mechanics would preserve incidental complexity and make the WPF application harder to maintain.
- Driving production forever from an imported sequence model would keep the project coupled to a migration artifact instead of a clear test-station domain model.

## Architecture

The system is split into six layers.

### TestPlanRepository

Loads and validates the WPF-owned stable test plan file.

It extracts:

- plan name and version
- product/model
- ordered test items
- item kind
- required/optional behavior
- stop-on-failure behavior
- timeouts
- item parameters
- limits
- report fields

### HistoricalReferenceImporter

Parses `profile.html` only for historical reference and old/new comparison tools.

It extracts:

- Sequence names
- Step names
- Step types
- Adapter names
- Run mode
- Description text
- Flow properties
- Pre/post expressions
- If/while/for conditions
- Limits
- Module calls

Raw fields are retained alongside parsed fields so no exported sequence information is lost when comparing against the old system. This layer is not part of the production execution path.

### FunctionalWorkflow

Owns the production test flow in domain terms:

- fixture preparation
- flashing
- SN acquisition
- safety checks
- FCT measurements
- limit judgment
- OK/NG output handling
- cleanup

The workflow should call explicit C# services and adapters. It may be generated or initially populated from imported reference data, but its public model should be understandable without knowing TestStand internals.

### MigrationRunner

Executes imported sequence definitions for offline smoke tests, migration coverage checks, and temporary operation while functional workflow modules are being extracted. It maintains a TestStand-like execution context only where needed to interpret the exported sequence:

- `FileGlobals`
- `Locals`
- `Parameters`
- `RunState`
- step results
- sequence failed flag
- stop requested flag

The migration runner supports:

- Setup / Main / Cleanup execution
- `Run Mode: Skip`
- `Wait`
- `If / Else`
- `While`
- `For`
- `Sequence Call`
- post-step result handling
- failure termination
- safe cleanup

### StepExecutors

Each supported TestStand step type has a dedicated executor for the migration runner:

- `ActionStepExecutor`
- `NumericLimitStepExecutor`
- `StringValueStepExecutor`
- `PassFailStepExecutor`
- `MultipleNumericLimitStepExecutor`
- `WaitStepExecutor`
- `IfStepExecutor`
- `WhileStepExecutor`
- `ForStepExecutor`
- `SequenceCallStepExecutor`

### Adapters

Adapters replace TestStand adapters and LabVIEW execution in both the migration runner and the final functional workflow.

Existing .NET modules are used when appropriate:

- `Fct.ModbusRtu.ModbusRtuClient` -> `ModbusIoAdapter`
- `Fct.Acquire.YkDaq20081Client` -> `DaqVoltageAdapter`
- `USB2IICDll.TestStandApi` -> `UsbI2cAdapter`
- `Oscil.TestStandApi` / `ScopeMeasurementClient` -> `OscilloscopeAdapter`
- `SG.TestStandApi` -> `SignalGeneratorAdapter`
- `ReadConfigLib.JsonConfigReader` -> `ConfigAdapter`
- `PreparedArtifactPaths.PreparedArtifactPathsReader` -> `ArtifactPathAdapter`

LabVIEW steps are mapped by VI path and step name:

- `PowerControl.vi` -> `PowerSupplyAdapter`
- `WriteI2C.vi` -> `UsbI2cAdapter.WriteBytes`
- `ReadI2C.vi` -> `UsbI2cAdapter.ReadBytes`
- `ReadVolt.vi` -> `DaqVoltageAdapter.ReadVoltage`
- `ReadSN.vi` -> scanner/SN adapter
- download VIs -> `FlashAdapter`
- File IO VIs -> `ReportAdapter`
- MES VIs -> `MesAdapter`
- PLC VIs -> `PlcAdapter`

### WPF Application

The WPF layer provides the operator interface:

- Start / stop / reset
- SN display and input where applicable
- current step display
- sequence tree with live statuses
- result table
- real-time logs
- configuration screen
- manual debug screen

## Migration Flow Model

The importer generates this internal reference model:

```text
SequenceDefinition
- Name
- SetupSteps
- MainSteps
- CleanupSteps

StepDefinition
- Id
- Name
- StepType
- Adapter
- RunMode
- DescriptionRaw
- FlowPropertiesRaw
- PreExpression
- PostExpression
- ConditionExpression
- ModuleCall
- Limits
- Children / BlockEnd
```

Migration execution uses:

```text
ExecutionContext
- FileGlobals
- Locals
- Parameters
- RunState
- StepResults
- SequenceFailed
- StopRequested
```

Each step returns:

```text
StepResult
- StepName
- Status
- Value
- Unit
- LowLimit
- HighLimit
- Message
- StartTime
- EndTime
- Error
```

Supported result statuses:

- `Passed`
- `Failed`
- `Error`
- `Skipped`
- `Terminated`
- `Stopped`

## Flashing Design

Flashing remains external and script-driven.

Mappings:

```text
Support/Download/Test RFP_Flash_once.bat.vi
-> FlashAdapter.RunRfp()
-> RFP_Auto/Scripts/Flash_once.bat

Support/Download/Test RedCase_FlashUpdate_Run.bat.vi
-> FlashAdapter.RunTcon()
-> RedCase_Auto/Debug/FlashUpdate_Run.bat

Support/Download/Test TDDI_Flash_once.bat.vi
-> FlashAdapter.RunTddi()
-> TDDI_Auto/Test/Test/bin/Debug/flash_run.bat
```

`FlashAdapter` responsibilities:

- Set the correct working directory.
- Start the external process.
- Capture stdout and stderr.
- Capture exit code.
- Detect `PASS` / `FAIL` output.
- Use existing artifact path readers where applicable.
- Attach external log paths to final step results.

The Renesas RFP, RedCase, and TDDI flashing implementations are not rewritten.

## WPF User Experience

The first version is a production operator UI, not a configuration-heavy engineering suite.

Main screen:

- top status bar with SN, current mode, current step, progress, start/stop/reset
- left sequence tree grouped by sequence and section
- center result grid with values and limits
- bottom real-time log panel

Configuration screen:

- edits `Config.json`
- supports burn paths, COM ports, host addresses, and firmware paths
- does not include Excel recipe editing

Manual debug screen:

- read Modbus input channels
- write Modbus output channels
- control power channels
- run I2C read/write
- read oscilloscope values
- run flashing scripts individually

## Error Handling

Failure handling follows the product test intent represented by the exported sequence, with WPF-specific improvements where they make behavior safer or clearer.

Rules:

- `Run Mode: Skip` steps are not executed and return `Skipped`.
- limit failures return `Failed` and set `SequenceFailed`.
- process exit code failures return `Failed` or `Error` depending on context.
- device communication exceptions return `Error`.
- UI stop requests return `Stopped`.
- `SequenceFilePostStepFailure` behavior is implemented as sequence failure termination unless the imported sequence or a documented WPF rule explicitly allows continuation.

Cleanup must run after failures and stops where possible.

Required safe cleanup includes:

- turn off power outputs
- release fixture outputs according to imported cleanup steps
- close DAQ
- close Modbus
- close oscilloscope / signal generator / I2C resources
- preserve existing external process logs

## Logging And Reports

The application writes three logical log streams:

```text
RunnerLog
- step start/end
- expression evaluation
- adapter calls
- exceptions

ProcessLog
- external script stdout/stderr
- exit code
- external log file paths

ResultLog
- step result records
- values
- limits
- duration
```

Report outputs:

- CSV for production use
- JSON for complete traceability

File naming:

```text
{SN}_{yyyyMMdd_HHmmss}_{Pass|Fail}.csv
{SN}_{yyyyMMdd_HHmmss}_{Pass|Fail}.json
```

External flashing logs remain in their existing locations and are referenced from the final report.

## Platform

The first version should target Windows with WPF.

Because several existing DLLs are x86-only, the first implementation should build the WPF application as x86 unless those DLLs are isolated behind a separate 32-bit helper process.

Initial recommendation: build the WPF app as x86 to reduce integration risk.

## Verification Strategy

### Offline Verification

- Parse `profile.html`.
- Verify 12 sequences and 820 steps.
- Verify `MainSequence` structure.
- Verify `Run Mode: Skip`.
- Verify block parsing for if/else/while/for/end.
- Execute using mock adapters.
- Verify failure, stop, and cleanup behavior.

### Semi-Hardware Verification

- Validate Modbus IO only.
- Validate flashing scripts only.
- Validate I2C only.
- Validate DAQ only.
- Validate oscilloscope only.
- Validate signal generator only.

### Full-System Verification

- Run a unit through the old TestStand sequence and save results.
- Run the same unit through WPF hardware mode.
- Compare final PASS/FAIL, first failure if any, flashing logs, critical measurements, limits, safety actions, cleanup actions, and report output.
- Use step-by-step comparison only as a diagnostic tool. The release gate is functional equivalence, not a mechanical clone of TestStand execution.
- Fix unintended functional differences. Intentional WPF improvements must be documented with the reason and validation evidence.

## Implementation Phases

### Phase 1: Importer

- Parse `profile.html`.
- Generate `SequenceDefinition`.
- Export parsed sequence to JSON for review.

### Phase 2: Runner Core

- Implement execution context.
- Implement action, wait, if, while, for, end, sequence call.
- Implement skip, stop, failure, cleanup.

### Phase 3: Basic Adapters

- Config adapter
- Modbus IO adapter
- flashing process adapter
- result logging

### Phase 4: FCT Adapters

- USB I2C
- power control replacement
- DAQ voltage reading
- oscilloscope measurement
- signal generator

### Phase 5: WPF UI And Line Validation

- main operator screen
- configuration screen
- manual debug screen
- CSV/JSON report output
- old-vs-new execution comparison

### Phase 6: Functional Workflow Extraction

- convert migration-runner behavior into explicit workflow modules
- group imported steps into named production test items
- replace TestStand expression dependence with typed C# test parameters
- keep the importer and migration runner available for coverage checks until line validation is complete

## First Version Acceptance Criteria

- The application does not require the TestStand execution engine to run tests.
- The application does not execute LabVIEW VIs.
- Existing flashing `bat`/`ps1` scripts and external tools are preserved.
- The WPF workflow covers production-critical flashing, safety checks, FCT measurements, limits, result judgment, and cleanup from the original station, except for documented WPF improvements.
- A full product test can run from WPF and produce PASS/FAIL.
- Results include step-level records and external flashing log paths.
- Failure or stop triggers safe cleanup.

## Known Risks

- The exported `profile.html` may not contain every LabVIEW VI connector parameter. Where this occurs, the implementation must derive behavior from step names, expressions, existing VI context, or a more detailed export.
- Some existing DLLs are x86-only.
- TestStand expression compatibility is only required where the migration importer/runner still depends on exported expressions. The final workflow should prefer typed C# parameters and explicit test item inputs.
- Production readiness requires old-vs-new line validation plus a documented list of any intentional differences.
