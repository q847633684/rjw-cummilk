# Settings 目录说明

本目录将 `MilkCumSettings` 拆分为多个 partial 文件，目标是降低单文件复杂度并明确职责边界。

## 文件职责

- `MilkCumSettings.cs`
  - 主入口与基础设置字段
  - `ExposeData()` 主流程
  - `UpdateMilkCumSettings()` 应用设置到 Def/运行态
- `MilkCumSettings.UI.cs`
  - 设置窗口 UI 状态字段（Tab、Widget 引用）
  - `DoWindowContents()` 与子 Tab 分发
- `MilkCumSettings.Accessors.cs`
  - 对外查询/计算类访问器（如可泌乳判定、耐受因子、乳品标签）
- `MilkCumSettings.Data.cs`
  - 种族-产物映射、基因映射数据
  - 相关序列化与初始化
- `MilkCumSettings.Model.cs`
  - 压力/炎症/射乳反射/组织适应等模型参数
  - 相关公式方法与模型序列化
- `MilkCumSettings.Risk.cs`
  - `MilkRiskSettings` 桥接属性与风险项序列化入口
- `MilkCumSettings.Cum.cs`
  - Cum/Leak 设置字段与序列化入口
- `MilkRiskSettings.cs`
  - 风险设置数据类本体（`IExposable`）

## 维护约定

- `EM.*` 键名视为稳定接口，修改前请先确认影响范围。
- 新增设置时，优先放到职责最接近的 partial 文件。
- `ExposeData()` 只保留主流程调用，具体模块序列化放在对应分文件。
