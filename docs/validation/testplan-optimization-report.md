# Testplan Optimization Report

Generated from `Runtime/TestPlans/Rfp7000V2.testplan.json`.

## Summary

- Plan: RFP 7000 V2 v0.1.0
- Top-level items: 17
- Timeout total: 3730 seconds (62.2 minutes)
- Explicit settleMs total: 116500 ms (116.5 seconds)

## Kind Summary

| Kind | Count | Timeout Seconds |
| --- | ---: | ---: |
| Cleanup | 1 | 60 |
| FixturePrepare | 1 | 60 |
| Flash | 4 | 2400 |
| FunctionalCheck | 6 | 1080 |
| LimitCheck | 1 | 30 |
| Measurement | 2 | 60 |
| ResultOutput | 1 | 10 |
| SafetyCheck | 1 | 30 |

## Slowest Items

| ID | Timeout Seconds | Kind | Stop On Failure |
| --- | ---: | --- | --- |
| flash.mcu.simple | 600 | Flash | True |
| flash.tcon | 600 | Flash | True |
| flash.tddi | 600 | Flash | True |
| flash.mcu.shipping | 600 | Flash | True |
| fct.button.group | 210 | FunctionalCheck | True |
| fct.hvac-bklt.group | 180 | FunctionalCheck | True |
| fct.swpack-pwm.group | 180 | FunctionalCheck | True |
| fct.hvac-position.group | 180 | FunctionalCheck | True |

## Power-On Reuse

- CH1 12.2 V: 4 occurrence(s)
  - fct.hvac-sw.group
  - fct.hvac-ind.group
  - fct.swpack-pwm.group
  - fct.hvac-bklt.group
- CH3 3.3 V: 2 occurrence(s)
  - fct.hvac-position.group
  - fct.hvac-sw.group

## Settle Time

- Explicit settleMs total: 116500 ms
- fct.swpack-pwm.group/fct.swpack-pwm.pwm4/HI: 5000 ms
- fct.hvac-bklt.group/fct.hvac-bklt.amb/HI: 5000 ms
- fct.swpack-pwm.group/fct.swpack-pwm.pwm1/HI: 5000 ms
- fct.hvac-bklt.group/fct.hvac-bklt.bu/HI: 5000 ms
- fct.swpack-pwm.group/fct.swpack-pwm.pwm2/HI: 5000 ms
- fct.hvac-bklt.group/fct.hvac-bklt.rd/HI: 5000 ms
- fct.swpack-pwm.group/fct.swpack-pwm.pwm3/HI: 5000 ms
- fct.hvac-bklt.group/fct.hvac-bklt.wh/HI: 5000 ms

## I2C Reuse

- `0x12|readLength=|readRegister=0x0A|writeData=`: 14 occurrence(s)
  - fct.button.group/fct.button.s1/Open
  - fct.button.group/fct.button.s1/HiToLow
  - fct.button.group/fct.button.s2/Open
  - fct.button.group/fct.button.s2/HiToLow
  - fct.button.group/fct.button.s3/Open
- `0x12|readLength=5|readRegister=0xD4|writeData=`: 10 occurrence(s)
  - fct.hvac-sw.group/fct.hvac-sw.def-frt/LO
  - fct.hvac-sw.group/fct.hvac-sw.def-frt/HI
  - fct.hvac-sw.group/fct.hvac-sw.dfg-rr/LO
  - fct.hvac-sw.group/fct.hvac-sw.dfg-rr/HI
  - fct.hvac-sw.group/fct.hvac-sw.auto/LO
- `0x12|readLength=|readRegister=|writeData=00 00`: 9 occurrence(s)
  - fct.swpack-pwm.group/fct.swpack-pwm.pwm1/LO
  - fct.swpack-pwm.group/fct.swpack-pwm.pwm2/LO
  - fct.swpack-pwm.group/fct.swpack-pwm.pwm3/LO
  - fct.swpack-pwm.group/fct.swpack-pwm.pwm4/LO
  - fct.hvac-bklt.group/fct.hvac-bklt.wh/LO
- `0x12|readLength=|readRegister=|writeData=FF FF`: 9 occurrence(s)
  - fct.swpack-pwm.group/fct.swpack-pwm.pwm1/HI
  - fct.swpack-pwm.group/fct.swpack-pwm.pwm2/HI
  - fct.swpack-pwm.group/fct.swpack-pwm.pwm3/HI
  - fct.swpack-pwm.group/fct.swpack-pwm.pwm4/HI
  - fct.hvac-bklt.group/fct.hvac-bklt.wh/HI
- `0x12|readLength=|readRegister=|writeData=00`: 6 occurrence(s)
  - fct.hvac-ind.group/fct.hvac-ind.auto/LO
  - fct.hvac-ind.group/fct.hvac-ind.hvac-en/LO
  - fct.hvac-ind.group/fct.hvac-ind.def-frt/LO
  - fct.hvac-ind.group/fct.hvac-ind.dfg-rr/LO
  - fct.hvac-ind.group/fct.hvac-ind.ac-req/LO
- `0x12|readLength=|readRegister=`: 4 occurrence(s)
  - fct.i2c.debug-mode
  - fct.hvac-ind.group
  - fct.swpack-pwm.group
  - fct.hvac-bklt.group
- `0x12|readLength=7|readRegister=|writeData=FF 10 20 D1`: 2 occurrence(s)
  - fct.hvac-position.group/fct.hvac-position.tmp1-phsa/0V
  - fct.hvac-position.group/fct.hvac-position.tmp1-phsa/3.3V
- `0x12|readLength=7|readRegister=|writeData=FF 10 21 D0`: 2 occurrence(s)
  - fct.hvac-position.group/fct.hvac-position.tmp1-phsb/0V
  - fct.hvac-position.group/fct.hvac-position.tmp1-phsb/3.3V

## Stop-On-Failure Review

- result.output: stopOnFailure is not true
- cleanup.fixture: stopOnFailure is not true

## Optimization Priority Review

| Item | Status | Evidence | Next Action |
| --- | --- | --- | --- |
| Keep this report as the baseline | 可立即改 | Current report captures timeout, power, settle, and I2C reuse signals. | Re-run this script after every testplan change and compare the generated report. |
| Flash timeout audit | 需硬件确认 | Flash kind timeout total is 2400 seconds across 4 steps; review table is tracked in `docs/validation/flash-timeout-review.md`. | Fill actual duration, max observed duration, and suggested timeout after hardware timing data is available. |
| 5000 ms settle checks | 需硬件确认 | Explicit settleMs total is 116500 ms; review table is tracked in `docs/validation/settle-time-review.md`. | Use oscilloscope or device response data to fill suggested settleMs before reducing or moving waits to group level. |
| Repeated I2C read signatures | 需硬件确认 | Repeated I2C signatures are listed above, including button and HVAC switch groups. | Confirm product state does not change between reads before merging reads in the executor. |
| Shared power-on groups | 可立即改 | CH1 12.2 V and CH3 3.3 V reuse is visible at group level. | Keep new group-level power structure; avoid reintroducing child-level repeated power toggles. |
| result.output and cleanup.fixture stopOnFailure=false | 暂不改 | These are terminal/reporting and cleanup steps. | Keep running result output and cleanup even when the test has failed. |

## Optimization Suggestions

- Review flash item timeouts first; they dominate worst-case station time.
- Keep shared 3.3V power at group level where child checks use the same rail.
- Merge repeated I2C reads with the same address/readLength/register when the product state is unchanged.
- Confirm each explicit settleMs is required by signal settling; reduce or move it to group level when safe.
- Keep stopOnFailure=true for steps whose failure makes later results meaningless.
