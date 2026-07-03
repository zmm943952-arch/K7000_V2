# I2C Reuse Review

Generated from `Runtime/TestPlans/Rfp7000V2.testplan.json`.

## Summary

- I2C event count: 73
- Repeated signature count: 12
- Rule: do not merge I2C reads until hardware confirms product state is unchanged between candidate reads.

## Review Table

| Signature | Occurrences | Merge Candidate | Hardware Confirmed | Notes |
| --- | ---: | --- | --- | --- |
| `0x12|readLength=|readRegister=0x0A|writeData=` | 14 | Yes | No | Examples:<br>fct.button.group/fct.button.s1/Open<br>fct.button.group/fct.button.s1/HiToLow<br>fct.button.group/fct.button.s2/Open<br>fct.button.group/fct.button.s2/HiToLow |
| `0x12|readLength=5|readRegister=0xD4|writeData=` | 10 | Yes | No | Examples:<br>fct.hvac-sw.group/fct.hvac-sw.def-frt/LO<br>fct.hvac-sw.group/fct.hvac-sw.def-frt/HI<br>fct.hvac-sw.group/fct.hvac-sw.dfg-rr/LO<br>fct.hvac-sw.group/fct.hvac-sw.dfg-rr/HI |
| `0x12|readLength=|readRegister=|writeData=00 00` | 9 | Yes | No | Examples:<br>fct.swpack-pwm.group/fct.swpack-pwm.pwm1/LO<br>fct.swpack-pwm.group/fct.swpack-pwm.pwm2/LO<br>fct.swpack-pwm.group/fct.swpack-pwm.pwm3/LO<br>fct.swpack-pwm.group/fct.swpack-pwm.pwm4/LO |
| `0x12|readLength=|readRegister=|writeData=FF FF` | 9 | Yes | No | Examples:<br>fct.swpack-pwm.group/fct.swpack-pwm.pwm1/HI<br>fct.swpack-pwm.group/fct.swpack-pwm.pwm2/HI<br>fct.swpack-pwm.group/fct.swpack-pwm.pwm3/HI<br>fct.swpack-pwm.group/fct.swpack-pwm.pwm4/HI |
| `0x12|readLength=|readRegister=|writeData=00` | 6 | Yes | No | Examples:<br>fct.hvac-ind.group/fct.hvac-ind.auto/LO<br>fct.hvac-ind.group/fct.hvac-ind.hvac-en/LO<br>fct.hvac-ind.group/fct.hvac-ind.def-frt/LO<br>fct.hvac-ind.group/fct.hvac-ind.dfg-rr/LO |
| `0x12|readLength=|readRegister=` | 4 | Yes | No | Examples:<br>fct.i2c.debug-mode<br>fct.hvac-ind.group<br>fct.swpack-pwm.group<br>fct.hvac-bklt.group |
| `0x12|readLength=7|readRegister=|writeData=FF 10 20 D1` | 2 | Maybe | No | Examples:<br>fct.hvac-position.group/fct.hvac-position.tmp1-phsa/0V<br>fct.hvac-position.group/fct.hvac-position.tmp1-phsa/3.3V |
| `0x12|readLength=7|readRegister=|writeData=FF 10 21 D0` | 2 | Maybe | No | Examples:<br>fct.hvac-position.group/fct.hvac-position.tmp1-phsb/0V<br>fct.hvac-position.group/fct.hvac-position.tmp1-phsb/3.3V |
| `0x12|readLength=7|readRegister=|writeData=FF 10 22 CF` | 2 | Maybe | No | Examples:<br>fct.hvac-position.group/fct.hvac-position.tmp2-phsa/0V<br>fct.hvac-position.group/fct.hvac-position.tmp2-phsa/3.3V |
| `0x12|readLength=7|readRegister=|writeData=FF 10 23 CE` | 2 | Maybe | No | Examples:<br>fct.hvac-position.group/fct.hvac-position.tmp2-phsb/0V<br>fct.hvac-position.group/fct.hvac-position.tmp2-phsb/3.3V |
| `0x12|readLength=7|readRegister=|writeData=FF 10 26 CB` | 2 | Maybe | No | Examples:<br>fct.hvac-position.group/fct.hvac-position.fan-up-sw/0V<br>fct.hvac-position.group/fct.hvac-position.fan-up-sw/3.3V |
| `0x12|readLength=7|readRegister=|writeData=FF 10 27 CA` | 2 | Maybe | No | Examples:<br>fct.hvac-position.group/fct.hvac-position.fan-dn-sw/0V<br>fct.hvac-position.group/fct.hvac-position.fan-dn-sw/3.3V |

## Hardware Confirmation Instructions

1. Confirm each candidate read happens after the DUT state has settled.
2. Confirm no relay, power rail, or writeData changes make a later read intentionally different.
3. Only merge reads inside the executor after the repeated signature maps to the same physical state.
4. Re-run Mock validation and hardware spot checks after any I2C merge.
