# Testplan Hardware Confirmation Checklist

Generated from `Runtime/TestPlans/Rfp7000V2.testplan.json`.

## Summary

- No-Hardware Completed: Mock validation, report generation, static testplan sync, group power guard, and audit report generation.
- Hardware Required: Flash timeout tuning, settleMs reduction, I2C read merge confirmation, and final station cycle-time measurement.
- Flash steps: 4
- Explicit settleMs checks: 49; total 116500 ms
- Repeated I2C signatures: 12
- Continue-on-failure entries: 2

## Checklist

| Area | Status | Evidence | Hardware Action |
| --- | --- | --- | --- |
| Flash Timeout | Hardware Required | See `docs/validation/flash-timeout-review.md`. | Measure actual duration for all flash scripts and set suggested timeout with margin. |
| Settle Time | Hardware Required | See `docs/validation/settle-time-review.md`. | Measure signal/device settle time before reducing any explicit wait. |
| I2C Reuse | Hardware Required | See `docs/validation/i2c-reuse-review.md`. | Confirm repeated signatures read the same physical state before executor-level merge. |
| Stop Policy | No-Hardware Completed | See `docs/validation/stop-policy-review.md`. | Reconfirm only result output and cleanup should continue after failure. |
| Shared Power | No-Hardware Completed | Group-level 3.3 V and 12.2 V reuse is guarded by static tests. | Spot-check relay/power rail stability on real fixture. |
| Reporting | No-Hardware Completed | Mock validation checks CSV expected value, comparison type, sent, reply, and reason fields. | Run one real failed DUT and confirm production log traceability. |

## Hardware Run Order

1. Run the current testplan once without changing timing values and record total cycle time.
2. Fill actual/max/suggested values in Flash and settleMs review tables.
3. Validate I2C merge candidates one group at a time; do not merge reads that cross a state change.
4. Re-run `.\Tools\run-testplan-optimization-audit.ps1` and full `dotnet test` after each accepted timing or executor change.
