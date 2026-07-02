# Power Supply Adapter Design

## Goal

Replace the LabVIEW `PowerControl.vi` step with a direct C# TCP implementation.

## Behavior

`PowerControl.vi` steps continue to route to the `PowerSupply` adapter. The adapter parses step names such as `PowerOn-Channel1-12.2V`, `PowerOn-Channel3-3.3V`, and `PowerOff-Channel1-12.2V`.

Defaults match the confirmed TestStand/LabVIEW behavior:

- Host: `192.168.1.15`
- Port: `8088`
- Timeout: `2000 ms`
- Terminator: `\r\n`
- No response readback
- Any TCP/connect/write failure returns `StepStatus.Error`

## Commands

Power on sends:

```text
:SOURce{channel}:VOLTage {voltage}\r\n
:OUTPut{channel}:STATe ON\r\n
```

Power off sends:

```text
:OUTPut{channel}:STATe OFF\r\n
```

## Test Strategy

Use a fake transport to assert exact commands, host, port, timeout, and error propagation without connecting to the real instrument.
