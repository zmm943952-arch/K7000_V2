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
- 测试数量：157
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
