# TestPlan Execution Map

Date: 2026-06-30

Execution source:

- `src/RfpTestStation/Rfp7000V2.testplan.json`

## Current Execution Chain

The WPF app loads the configured test plan, converts each enabled item to `TestItem`, and runs them through `FunctionalTestWorkflow`.

In `Mock` mode, every test item returns a simulated pass result.

In `Hardware` mode:

- `Flash` items are executed by `FlashAdapter` through the configured external `.bat`/`.ps1` script path.
- `FixturePrepare`, `SafetyCheck`, `ResultOutput`, and `Cleanup` are wired to `IStationIoController`.
- `Measurement` items route `UsbI2c` and `Oscilloscope` operations to their hardware adapters and apply TestPlan limits when provided.
- `LimitCheck` items route `DaqVoltage` readings to the DAQ hardware adapter and apply TestPlan limits.
- The independent safety monitor is already wired in Hardware mode through `IStationIoController` and `StationSafetySupervisor`.

## TestPlan Item Status

| Order | ID | Kind | Current Hardware Behavior | Needed Wiring |
| --- | --- | --- | --- | --- |
| 1 | `fixture.prepare` | `FixturePrepare` | Writes Y1/channel 1 false, then Y2/channel 2 true. | Verify physical relay polarity on the station. |
| 2 | `flash.mcu.simple` | `Flash` | Real external script call | Verify configured path, working directory, timeout, and output pass/fail token rules on station. |
| 3 | `flash.tcon` | `Flash` | Real external script call | Verify configured path and arguments for TCON bin selection. |
| 4 | `flash.tddi` | `Flash` | Real external script call | Verify configured path and TDDI tool runtime dependencies. |
| 5 | `flash.mcu.shipping` | `Flash` | Real external script call | Verify same RFP script can distinguish simple/shipping mode or add arguments. |
| 6 | `safety.fixture-position` | `SafetyCheck` | Reads configured input channel and compares expected value. | Verify X2/channel 2 meaning is actual fixture-down position. |
| 7 | `fct.i2c.debug-mode` | `Measurement` | Routes `adapter=UsbI2c` and `operation=EnterAndCheckDebugMode` to `UsbI2cAdapter`. | Verify USB I2C DLL/runtime and target debug-mode response on station. |
| 8 | `fct.oscilloscope.vavg` | `Measurement` | Routes `adapter=Oscilloscope`, `operation=ReadVavg`, and `channel`; applies `low`/`high`/`unit`. | Verify oscilloscope host, channel mapping, and expected voltage range on station. |
| 9 | `fct.ac-input.3.hi` | `LimitCheck` | Routes `adapter=DaqVoltage` and `channel`; applies `low`/`high`/`unit`. | Verify DAQ COM port/slave ID and physical AC input channel mapping on station. |
| 10 | `result.output` | `ResultOutput` | Writes OK channel 3 true and NG channel 4 false when pass; reverses them when any blocking failure occurred. | Verify OK/NG pulse or latch timing requirements on the station. |
| 11 | `cleanup.fixture` | `Cleanup` | Writes Y2/channel 2 false, Y1/channel 1 false, waits, then Y1/channel 1 true. | Add power-off actions after power-supply integration. |

## Existing Adapters Available

| Adapter | Existing Implementation | TestPlan Direct Use |
| --- | --- | --- |
| `ModbusIoAdapter` | Real `Fct.ModbusRtu.ModbusRtuClient` input/output calls plus `IStationIoController`. | Safety monitor plus fixture/result/cleanup testplan items use it. |
| `FlashAdapter` | Real external process execution with timeout, cancellation, exit-code and output-token status mapping. | Already wired for `Flash`. |
| `UsbI2cAdapter` | Calls `USB2IICDll.TestStandApi` for debug mode and byte read/write operations. | Wired for `Measurement` when `adapter=UsbI2c`. |
| `OscilloscopeAdapter` | Calls `Oscil.TestStandApi` for Vavg/frequency/duty cycle. | Wired for `Measurement` when `adapter=Oscilloscope`. |
| `DaqVoltageAdapter` | Calls `Fct.Acquire.YkDaq20081Client.ReadVoltage`. | Wired for `LimitCheck` when `adapter=DaqVoltage`. |
| `PowerSupply`, `SerialNumber`, `Mes`, `Plc` | Explicit unsupported placeholders. | Not in current testplan, but still needed later for full old-flow parity. |

## Recommended Next Implementation Order

1. Run semi-hardware validation for `UsbI2c`, `Oscilloscope`, and `DaqVoltage` one subsystem at a time.
2. Extend `Flash` script mapping so `flashKind` can drive arguments or mode-specific scripts when needed.
3. Run a Hardware dry run with actual IO disabled or on a safe bench fixture, validating stop/cancel behavior during long flash scripts.
4. Only after the basic loop is stable, wire PowerSupply, SerialNumber, MES, and PLC replacements.

## Open Risks

- Hardware registry currently uses hard-coded defaults such as `COM2`, `COM4`, and oscilloscope host defaults. These should be read from `Project/Config.json` before station use.
- Result output now receives workflow-level blocking-failure state. Pulse/latch timing still needs station confirmation.
- Cleanup runs after normal failures. Safety-trigger cancellation behavior should be verified on a safe bench fixture.
