# Power Supply Adapter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the LabVIEW `PowerControl.vi` power control with a C# TCP power supply adapter.

**Architecture:** Add a focused `PowerSupplyAdapter` in the existing hardware adapter layer. Use a small transport interface so command formatting can be tested without real hardware, while production uses `TcpClient`.

**Tech Stack:** C#/.NET Framework 4.8, xUnit, TCP/SCPI-style text commands.

---

### Task 1: Power Supply Adapter

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Adapters/Hardware/PowerSupplyAdapter.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Adapters/Hardware/HardwareStationAdapterRegistry.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Tests/Adapters/HardwareAdapterRegistryTests.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Adapters/PowerSupplyAdapterTests.cs`

- [ ] Write failing tests for PowerOn command formatting.
- [ ] Write failing tests for PowerOff command formatting.
- [ ] Write failing tests for malformed step names and transport exceptions.
- [ ] Implement `PowerSupplyAdapter` with default host, port, timeout, and CRLF terminator.
- [ ] Replace the hardware registry placeholder with `PowerSupplyAdapter`.
- [ ] Run the focused test suite.
- [ ] Run the solution test suite.
