# K7000 V2 Test Station

K7000 V2 产线测试/烧录工站项目。仓库包含新的 `RfpTestStation` WPF 测试站程序、运行时烧录资产、硬件适配库、历史 TestStand/LabVIEW 参考工程，以及验证文档。

## 目录结构

- `src/RfpTestStation/`：当前主程序源码，包含 WPF App、Core、Adapters、Tests。
- `Runtime/`：当前应用使用的运行时资产，部署时需要随程序一起拷贝。
- `Project/`：原始/历史工程与硬件工具，包括烧录脚本、TestStand、LabVIEW、配置工具等。
- `Tools/`：打包和发布辅助脚本。
- `docs/`：设计计划、验证清单和测试执行说明。
- `Reports/`：本地运行生成的报告目录，不纳入版本控制。
- `Deploy/`：本地发布包目录，不纳入版本控制。

## 构建与测试

主解决方案：

```powershell
dotnet test .\src\RfpTestStation\RfpTestStation.sln --nologo
```

当前基线验证结果：

- 测试项目：`RfpTestStation.Tests`
- 测试数量：190
- 最近验证：全部通过

## 运行时资产

`Runtime/` 是当前站点运行时目录，核心内容包括：

- `Runtime/Config/Config.json`
- `Runtime/TestPlans/Rfp7000V2.testplan.json`
- `Runtime/Flash/RFP_Auto`
- `Runtime/Flash/RedCase_Auto`
- `Runtime/Flash/TDDI_Auto`
- `Runtime/Lib/Hardware`

路径规则见 `Runtime/README.md`。部署时应复制完整应用目录，并保留同级 `Runtime/` 和 `Reports/`。

## 发布与版本控制

远端仓库：

```text
https://github.com/zmm943952-arch/K7000_V2.git
```

约定：

- 大更新完成后直接提交并推送到 `main`。
- 每次大更新同步维护本 README，至少补充变更内容、验证结果或运行注意事项。
- 不提交 `Deploy/`、`Reports/`、日志、备份、`bin/obj` 等生成物。
- 运行时必需的烧录工具、固件、硬件 DLL 会随仓库保留。

## 无硬件阶段

当前没有硬件时，优先把 Mock 模式、失败原因、报告链路跑扎实：

1. 用 Mock 模式跑完整 testplan，确认运行页、失败明细、结果统计、报告输出都能闭环。
2. 在运行页选择 Mock 场景，模拟 `Passed`、`Failed`、`Error`、`Stopped` 等状态。
3. 对每个关键失败场景填写具体 `reason`，确认界面和 CSV/JSON/LOG 报告都能看到同一原因。
4. 继续压缩 testplan 重复动作，减少无意义等待和重复上电。
5. 等硬件到位后，再按 `docs/validation/testplan-execution-map.md` 做 `UsbI2c`、`Oscilloscope`、`DaqVoltage` 半硬件验证。

Mock 场景文件放在 `Runtime/MockScenarios/*.mockscenario.json`。正式 testplan 不需要写 `mock`，运行时会按测试项 ID 临时合并场景：

```json
{
  "name": "DAQ voltage low",
  "items": {
    "fct.ac-input.3.hi": {
      "status": "Failed",
      "reason": "Mock DAQ voltage outside limits: channel=3; expected=3.135..3.465V; actual=2.900V",
      "value": 2.9,
      "low": 3.135,
      "high": 3.465,
      "unit": "V"
    }
  }
}
```

当前内置场景：

- `DAQ voltage low`
- `Fixture position failed`
- `I2C debug no response`
- `TCON flash failed`

无硬件阶段可以一键运行 Mock 验收：

```powershell
.\Tools\run-mock-validation.ps1
```

该脚本会自动跑完整通过流程，以及 `TCON flash failed`、`I2C debug no response`、`DAQ voltage low` 三个典型失败场景，并检查 CSV 中的 `Expected Value`、`Comparison Type`、`Sent`、`Reply`、`Reason` 等关键追溯字段。

也可以一键分析 testplan 的时间和重复动作：

```powershell
.\Tools\analyze-testplan.ps1
```

该脚本会输出 timeout 总和、按类型统计、最耗时步骤、重复 `powerOnBefore`、显式 `settleMs`、重复 I2C 签名，以及下一步 testplan 优化建议。
如需生成 Markdown 报告：

```powershell
.\Tools\analyze-testplan.ps1 -MarkdownPath docs\validation\testplan-optimization-report.md
```

Flash timeout 审核表可用下面命令生成：

```powershell
.\Tools\generate-flash-timeout-review.ps1
```

settleMs 审核表可用下面命令生成：

```powershell
.\Tools\generate-settle-time-review.ps1
```

I2C 重复读审核表可用下面命令生成：

```powershell
.\Tools\generate-i2c-reuse-review.ps1
```

失败中断策略审核表可用下面命令生成：

```powershell
.\Tools\generate-stop-policy-review.ps1
```

硬件到位后的确认清单可用下面命令生成：

```powershell
.\Tools\generate-hardware-confirmation-checklist.ps1
```

完整 testplan 优化审计可用下面命令一次性执行：

```powershell
.\Tools\run-testplan-optimization-audit.ps1
```

该脚本会重新生成 testplan 优化报告、Flash timeout 审核表、settleMs 审核表、I2C 重复读审核表、失败中断策略审核表、硬件确认清单，并运行 Mock 验收。

## 报告格式

CSV 报告文件名使用生产格式：

```text
<SN>_<yyyyMMddHHmmss>_<Passed|Failed>.csv
```

时间戳取本次测试结束时间。CSV 内容会先写入 `[SN]`、`[Result]`、`[Start Time]`、`[End Time]`、`[Test Time]`、`[UserName]`、`[Authority]`、`[Station]`，随后逐项记录 `Sent` 和 `Reply`，用于追溯每一步发了什么、设备/脚本回复了什么。

## 更新记录

- 2026-07-03：新增 I2C 重复读审核表生成脚本 `Tools/generate-i2c-reuse-review.ps1` 和 `docs/validation/i2c-reuse-review.md`，列出 12 类重复 I2C 签名和合并确认字段。
- 2026-07-03：新增一键 testplan 优化审计脚本 `Tools/run-testplan-optimization-audit.ps1`，可一次生成全部优化报告并运行 Mock 验收。
- 2026-07-03：新增失败中断策略审核脚本 `Tools/generate-stop-policy-review.ps1` 和 `docs/validation/stop-policy-review.md`，明确只有 `result.output` 与 `cleanup.fixture` 可失败后继续。
- 2026-07-03：新增硬件确认清单生成脚本 `Tools/generate-hardware-confirmation-checklist.ps1` 和 `docs/validation/testplan-hardware-confirmation-checklist.md`，汇总无硬件已完成项和硬件到位后必须确认的优化项。
- 2026-07-03：新增 settleMs 审核表生成脚本 `Tools/generate-settle-time-review.ps1` 和 `docs/validation/settle-time-review.md`，列出 49 处显式等待和硬件确认字段。
- 2026-07-03：新增 Flash timeout 审核表生成脚本 `Tools/generate-flash-timeout-review.ps1` 和 `docs/validation/flash-timeout-review.md`；新增静态测试校验 Flash 步骤的 script、timeout、stopOnFailure 和脚本存在性。
- 2026-07-03：新增 testplan 同步防回归测试，确保 `src/RfpTestStation/Rfp7000V2.testplan.json` 与 `Runtime/TestPlans/Rfp7000V2.testplan.json` 内容保持一致。
- 2026-07-03：新增 testplan 防回归测试，校验 Functional Group 子项和子检查不重新引入 `powerOnBefore`，守住组级共享上电优化。
- 2026-07-03：testplan 优化报告新增 `Optimization Priority Review` 分级表，将优化点标为“可立即改 / 需硬件确认 / 暂不改”，避免无硬件阶段误改真实时序。
- 2026-07-02：`Tools/analyze-testplan.ps1` 支持 `-MarkdownPath`，已生成 `docs/validation/testplan-optimization-report.md`，用于保存当前 testplan 时间与重复动作分析基线。
- 2026-07-02：新增 testplan 静态分析脚本 `Tools/analyze-testplan.ps1`，无硬件时可直接统计 timeout、重复上电、settleMs、I2C 重复签名和 stopOnFailure 风险点，作为测试时间优化清单。
- 2026-07-02：新增 Mock 自动验收脚本 `Tools/run-mock-validation.ps1` 和对应自动化测试；无硬件时可自动跑完整 Passed 流程、烧录失败、I2C 无回复、电压超限，并审核 CSV 追溯字段。
- 2026-07-02：主运行界面移除“运行模式/当前模式”显示；运行模式仍在设置页配置，避免操作员主界面信息过多。
- 2026-07-02：运行页左侧新增当前调用 testplan 显示，内容包含计划名称和 `Runtime/TestPlans/*.testplan.json` 路径；长路径自动换行，便于操作员确认本次测试实际使用的测试序列。
- 2026-07-02：CSV/JSON/LOG 报告文件名改为 `<SN>_<yyyyMMddHHmmss>_<Passed|Failed>`，时间戳使用结束时间；CSV 增加生产格式头信息，并在每个测试项记录 `Sent` 和 `Reply`。
- 2026-07-02：Mock 场景选择从主运行页移到设置页启动配置，运行页只保留当前运行模式，避免操作员在主测试界面误切故障场景。
- 2026-07-02：失败明细和报告增加 `ExpectedValue`、`CompareType`、`Target` 诊断字段；Mock 场景和安全检查失败不再只显示实际值，可同时显示期望值、比较方式和对象/通道。
- 2026-07-02：新增运行页 Mock 场景选择；场景文件放在 `Runtime/MockScenarios`，可按测试项 ID 注入失败、错误、停止或测量值，不再需要手动修改正式 testplan。
- 2026-07-02：增强 Mock 模式，测试项可通过 `mock.status/reason/value/low/high/unit` 注入失败、错误或停止结果；无硬件时可验证失败原因在界面和报告中的完整链路。
- 2026-07-02：运行页“产品名称”下拉框内容改为居中显示，和语言选择框的对齐方式保持一致。
- 2026-07-02：修复顶部当前时间不刷新的问题；主窗口增加 1 秒 `DispatcherTimer`，定时刷新 `CurrentTimeText`。
- 2026-07-02：进一步优化 testplan 供电流程：`HVAC Position Group` 与 `HVAC Switch Group` 的 3.3V 改为组级共享上电，删除子项 check 内重复 3.3V 上电动作；`Button Group` 的 3.3V 开关逻辑暂时保留，避免影响按键状态测试。
- 2026-07-02：运行页“产品名称”改为固定下拉选择，仅支持 `K7000`、`K7048`、`K7049`；旧配置中的非法产品名会回退为 `K7000`。
- 2026-07-02：测试计划维护页右侧详情中的 `Parameters` 改为缩进格式化显示，长 JSON 不再压缩成一整行；无效 JSON 仍保留原文便于排查。
- 2026-07-02：修复测试计划维护页新增只读显示字段后 exe 启动崩溃的问题；`SummaryText` 与 `SelectedTestPlanDetailText` 改为 OneWay 绑定，并新增 XAML 防回归测试。已重新生成 `Deploy/RfpTestStation_devtest` 发布包。
- 2026-07-02：优化测试计划维护页：测试项表格新增摘要列并隐藏长路径/限制字段，选中行后在右侧显示完整参数详情和功能组子项；保存前增加聚合校验原因列表，能一次显示重复 ID、超时、脚本路径、limit、group 子项等具体问题。
- 2026-07-02：优化 `Rfp7000V2.testplan.json`，将 33 个重复 FunctionalCheck 折成 6 个 `I2cFunctionalGroup` 分组（位置、HVAC 开关、指示灯、按钮、PWM、背光）。分组级共享供电动作，子项继续保留独立 relay/channel/check/limit，失败时仍显示具体子项和检查原因；`Runtime/TestPlans/Rfp7000V2.testplan.json` 已同步。
- 2026-07-02：运行页错误状态不再只显示“错误/失败”，会在顶部状态后追加当前具体原因；运行前置检查失败时也会写入失败明细表，并在失败明细中新增 `Reason` 列显示步骤原因。
