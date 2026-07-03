# Settle Time Review

Generated from `Runtime/TestPlans/Rfp7000V2.testplan.json`.

## Summary

- Explicit settleMs count: 49
- Explicit settleMs total: 116500 ms (116.5 seconds)
- Rule: do not reduce settleMs until signal/device response data is recorded.

## Review Table

| Scope | Level | Current settleMs | Hardware Confirmed | Suggested settleMs | Notes |
| --- | --- | ---: | --- | --- | --- |
| fct.hvac-bklt.group/fct.hvac-bklt.amb/HI | Check | 5000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-bklt.group/fct.hvac-bklt.bu/HI | Check | 5000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-bklt.group/fct.hvac-bklt.rd/HI | Check | 5000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-bklt.group/fct.hvac-bklt.wh-fan/HI | Check | 5000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-bklt.group/fct.hvac-bklt.wh/HI | Check | 5000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.swpack-pwm.group/fct.swpack-pwm.pwm1/HI | Check | 5000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.swpack-pwm.group/fct.swpack-pwm.pwm2/HI | Check | 5000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.swpack-pwm.group/fct.swpack-pwm.pwm3/HI | Check | 5000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.swpack-pwm.group/fct.swpack-pwm.pwm4/HI | Check | 5000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s3/HiToLow | Check | 3000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s6/HiToLow | Check | 3000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s1/HiToLow | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.wakesp/HiToLow | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-bklt.group/fct.hvac-bklt.amb/LO | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-bklt.group/fct.hvac-bklt.bu/LO | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-bklt.group/fct.hvac-bklt.rd/LO | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-bklt.group/fct.hvac-bklt.wh-fan/LO | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-bklt.group/fct.hvac-bklt.wh/LO | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.fan-dn-sw/0V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.fan-dn-sw/3.3V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.fan-up-sw/0V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.fan-up-sw/3.3V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.tmp1-phsa/0V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.tmp1-phsa/3.3V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.tmp1-phsb/0V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.tmp1-phsb/3.3V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.tmp2-phsa/0V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.tmp2-phsa/3.3V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.tmp2-phsb/0V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-position.group/fct.hvac-position.tmp2-phsb/3.3V | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-sw.group/fct.hvac-sw.ac-req/HI | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-sw.group/fct.hvac-sw.auto/HI | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-sw.group/fct.hvac-sw.def-frt/HI | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-sw.group/fct.hvac-sw.dfg-rr/HI | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.hvac-sw.group/fct.hvac-sw.recir/HI | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.swpack-pwm.group/fct.swpack-pwm.pwm1/LO | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.swpack-pwm.group/fct.swpack-pwm.pwm2/LO | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.swpack-pwm.group/fct.swpack-pwm.pwm3/LO | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.swpack-pwm.group/fct.swpack-pwm.pwm4/LO | Check | 2000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s1/Open | Check | 1000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s2/HiToLow | Check | 1000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s2/Open | Check | 1000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s3/Open | Check | 1000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s4/HiToLow | Check | 1000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s4/Open | Check | 1000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s5/HiToLow | Check | 1000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s5/Open | Check | 1000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.button.s6/Open | Check | 1000 | No | TBD | Needs hardware confirmation before reducing. |
| fct.button.group/fct.wakesp/Open | Check | 500 | No | TBD | Needs hardware confirmation before reducing. |

## Hardware Timing Instructions

1. Scope or log the signal/device response for every 5000 ms wait first.
2. Record stable response time and margin before reducing a wait.
3. If multiple checks share the same physical settling condition, consider moving the wait to group level after hardware confirmation.
4. Re-run Tools/analyze-testplan.ps1 -MarkdownPath docs\\validation\\testplan-optimization-report.md after any change.
