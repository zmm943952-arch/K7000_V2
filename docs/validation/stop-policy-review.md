# Stop Policy Review

Generated from `Runtime/TestPlans/Rfp7000V2.testplan.json`.

## Summary

- Top-level item count: 17
- Continue-on-failure count: 2
- Unexpected continue-on-failure count: 0
- Rule: only result output and cleanup may continue after a previous failure.

## Review Table

| ID | Kind | Policy | Allowed To Continue | Reason |
| --- | --- | --- | --- | --- |
| fixture.prepare | FixturePrepare | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| safety.fixture-position | SafetyCheck | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| flash.mcu.simple | Flash | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| flash.tcon | Flash | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| flash.tddi | Flash | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| flash.mcu.shipping | Flash | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| fct.i2c.debug-mode | Measurement | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| fct.hvac-position.group | FunctionalCheck | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| fct.hvac-sw.group | FunctionalCheck | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| fct.hvac-ind.group | FunctionalCheck | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| fct.button.group | FunctionalCheck | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| fct.swpack-pwm.group | FunctionalCheck | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| fct.hvac-bklt.group | FunctionalCheck | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| fct.oscilloscope.vavg | Measurement | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| fct.ac-input.3.hi | LimitCheck | Stop On Failure | No | Critical test step; later results are not meaningful if this step fails. |
| result.output | ResultOutput | Continue On Failure | Yes | Result output must run so a failed unit still produces the production CSV/JSON/LOG record. |
| cleanup.fixture | Cleanup | Continue On Failure | Yes | Fixture cleanup must run after failures to leave relays and fixture outputs in a known state. |
