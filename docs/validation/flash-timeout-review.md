# Flash Timeout Review

Generated from `Runtime/TestPlans/Rfp7000V2.testplan.json`.

## Summary

- Flash step count: 4
- Current flash timeout total: 2400 seconds
- Rule: do not reduce timeout until real hardware duration data is recorded.

## Review Table

| Step ID | Name | Flash Kind | Script | Script Exists | Current Timeout Seconds | Actual Duration Seconds | Max Observed Seconds | Suggested Timeout Seconds | Hardware Confirmed | Notes |
| --- | --- | --- | --- | --- | ---: | --- | --- | --- | --- | --- |
| flash.mcu.simple | MCU Simple Flash | RfpMcuSimple | `Runtime/Flash/RFP_Auto/Scripts/Flash_once.bat` | Yes | 600 | TBD | TBD | TBD | No | Need hardware timing data. |
| flash.tcon | TCON Flash | RedCase | `Runtime/Flash/RedCase_Auto/Debug/FlashUpdate_Run.bat` | Yes | 600 | TBD | TBD | TBD | No | Need hardware timing data. |
| flash.tddi | TDDI Flash | Tddi | `Runtime/Flash/TDDI_Auto/Test/Test/bin/Debug/flash_run.bat` | Yes | 600 | TBD | TBD | TBD | No | Need hardware timing data. |
| flash.mcu.shipping | MCU Shipping Flash | RfpMcuShipping | `Runtime/Flash/RFP_Auto/Scripts/Flash_once.bat` | Yes | 600 | TBD | TBD | TBD | No | Need hardware timing data. |

## Hardware Timing Instructions

1. Run each flash step at least 10 times on representative hardware.
2. Record actual duration and max observed duration.
3. Suggested timeout should include margin for slow station PC, retry behavior, and firmware size variance.
4. Update testplan timeout only after hardware confirmation is recorded in this file.
