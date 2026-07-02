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
- 测试数量：166
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

## 当前下一步

建议按 `docs/validation/testplan-execution-map.md` 推进：

1. 单独做 `UsbI2c` 半硬件验证。
2. 单独做 `Oscilloscope` 半硬件验证。
3. 单独做 `DaqVoltage` 半硬件验证。
4. 在安全工装上做 Hardware dry run，重点验证长时间烧录时的停止/取消行为。
5. 基本闭环稳定后，再接入 `PowerSupply`、`SerialNumber`、`MES`、`PLC`。

## 更新记录

- 2026-07-02：进一步优化 testplan 供电流程：`HVAC Position Group` 与 `HVAC Switch Group` 的 3.3V 改为组级共享上电，删除子项 check 内重复 3.3V 上电动作；`Button Group` 的 3.3V 开关逻辑暂时保留，避免影响按键状态测试。
- 2026-07-02：运行页“产品名称”改为固定下拉选择，仅支持 `K7000`、`K7048`、`K7049`；旧配置中的非法产品名会回退为 `K7000`。
- 2026-07-02：测试计划维护页右侧详情中的 `Parameters` 改为缩进格式化显示，长 JSON 不再压缩成一整行；无效 JSON 仍保留原文便于排查。
- 2026-07-02：修复测试计划维护页新增只读显示字段后 exe 启动崩溃的问题；`SummaryText` 与 `SelectedTestPlanDetailText` 改为 OneWay 绑定，并新增 XAML 防回归测试。已重新生成 `Deploy/RfpTestStation_devtest` 发布包。
- 2026-07-02：优化测试计划维护页：测试项表格新增摘要列并隐藏长路径/限制字段，选中行后在右侧显示完整参数详情和功能组子项；保存前增加聚合校验原因列表，能一次显示重复 ID、超时、脚本路径、limit、group 子项等具体问题。
- 2026-07-02：优化 `Rfp7000V2.testplan.json`，将 33 个重复 FunctionalCheck 折成 6 个 `I2cFunctionalGroup` 分组（位置、HVAC 开关、指示灯、按钮、PWM、背光）。分组级共享供电动作，子项继续保留独立 relay/channel/check/limit，失败时仍显示具体子项和检查原因；`Runtime/TestPlans/Rfp7000V2.testplan.json` 已同步。
- 2026-07-02：运行页错误状态不再只显示“错误/失败”，会在顶部状态后追加当前具体原因；运行前置检查失败时也会写入失败明细表，并在失败明细中新增 `Reason` 列显示步骤原因。
