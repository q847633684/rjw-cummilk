# 实现总结：统一配置、汉化、修复、RJW 与冗余

## 1. 统一配置 ✅

- **Cumpilation 两套设置**已并入主 mod 的**一个设置窗口**：
  - 在「选项 → Equal Milking」中新增 **Tab「Cumpilation - Base Settings」**（文案仍用 `cumpilation_settings_menuname` 翻译）。
  - 该 Tab 内由上到下：**Cumpilation 主设置**（膨胀/填充/覆盖、清洁收集、振荡、调试日志）+ **泄精设置**（污物、自动泄精到桶/清洁/随地、隐私、各倍数）。
- **数据**：所有 Cumpilation / Leaking 的选项字段已迁入 `EqualMilkingSettings`，并用 `ExposeData` 统一读写（键名带 `Cumpilation.` / `CumpilationLeak.` 前缀）。
- **兼容**：`Cumpilation.Settings` 与 `Cumpilation.Leaking.Settings` 改为**仅做代理**，静态属性读写在 `EqualMilkingSettings` 的对应静态字段上，原有 Cumpilation 代码无需改。
- **入口**：已删除 `Cumpilation_SettingsController` 与 `LeakCum_SettingsController` 两个 Mod 类，选项里不再出现两个单独的 Cumpilation 项。

---

## 2. 汉化统一 ✅

- **简体中文**：
  - 新增 `Languages/ChineseSimplified/Keyed/Mod_Settings.xml`：包含全部 `cumpilation_settings_*`、`cumpilation_cumsettings_*` 键的简中翻译。
  - 新增 `Languages/ChineseSimplified/Keyed/UI_Elements.xml`：包含 `cumpilation_button_cumseal_*`、`cumpilation_button_cumdeflate_*` 的简中翻译。
- **Milk_Dev 列**：PawnColumnDef 的 label 改为 Keyed 键 `EM.Milk_Dev`，并在 `Languages/English/Keyed/lang.xml` 与 `Languages/ChineseSimplified/Keyed/lang.xml` 中增加对应条目。
- **说明**：繁体中文、日文未新增 Mod_Settings/UI_Elements，若需可参照英文与简中结构补档。

---

## 3. 不合理处修复 ✅

- **HumanMilik.xml**：已重命名为 `HumanMilk.xml`（Def 内 defName 仍为 EM_HumanMilk，未改）。
- **调试日志默认值**：新字段 `Cumpilation_EnableDebugLogging` 默认 `false`，Scribe 默认也为 `false`。
- **Milk_Dev 硬编码**：已改为使用 Keyed 键 `EM.Milk_Dev`，并在英/简中 lang 中补全。
- **GlobaleBukkakeModifier**：Cumpilation 代码中仍通过 `Settings.GlobaleBukkakeModifier` 访问，为保持 API 兼容，代理属性名未改；实际存储字段名为 `Cumpilation_GlobalBukkakeModifier`（拼写正确）。

---

## 4. 精液/母乳与 RJW 的运用 + 可扩展方向

### 当前 RJW 运用情况

- **母乳 / 奶**  
  - 产奶类型：`ExtensionHelper.MilkDef` 被 RJW Patch 覆盖，若小人有 RJW 乳房且 `Fluid.consumable` 非原版 Milk，则返回该 consumable（如 EM_HumanMilk 或 rjw-genes 的奶）。  
  - 产奶量：`GetMilkAmount` 与 RJW 乳房 `FluidMultiplier` 联动；乘数为 0 时记入 Alert_FluidMultiplier。  
  - 产后：RJW Patch 在 `Hediff_BasePregnancy.PostBirth` 时为人形产奶者补 Lactating、允许哺乳。  
  - 泌乳期与 RJW：RJWLactatingBreastSize 在泌乳时增大 RJW 胸部 Severity；RJWLustIntegration 处理哺乳/吸奶后的性需求、泌乳期对 Need_Sex 的持续加成；RJWSexAndFertility 处理哺乳后性行为额外满足、泌乳期怀孕概率乘数（rjwLactationFertilityFactor）。  
  - 设置：RJW 相关选项仅在 `ModLister.GetModWithIdentifier("rim.job.world") != null` 时显示。

- **精液 / Cumpilation**  
  - 性行为与流体：Cumpilation 使用 RJW 的性行为与 SexFluidDef、部位（PartHelper 阴茎/阴道/肛门）、CompRJW、GenderUtility 等，实现膨胀/填充/覆盖、射精进收集建筑、泄精到桶等。  
  - 产主：射精者 → 收集建筑、泄精时从 HediffComp_SourceStorage 取来源，精液物品打 CompShowProducer，与奶制品共用“谁可以吃”规则。

### 可添加功能 / 优化建议（简述）

- **功能**  
  - 在奶/精液表格或角色面板显示“最近哺乳/射精时间”或“当前 RJW 流体倍率”（可只对开发模式或可选）。  
  - 设置项：泌乳期对 RJW 性欲/性满足的倍率可调（当前为固定逻辑）。  
  - 精液桶/泵：列表或筛选中显示“产主”或“混合”，便于区分单人桶与混合桶。  
  - 与 rjw-genes 的进一步联动：例如某基因产奶类型与“产主限制”的预设联动（若 rjw-genes 有对应 Def）。

- **优化**  
  - 收集建筑搜索距离（MaxGatheringCheckDistance）在大地图/大房间的性能：已有时可考虑按房间大小或建筑数量做简单缓存/节流。  
  - RJW 未加载时，所有 RJW 相关 Patch 与 UI 不执行，避免多余分支（当前已按 Mod 存在判断）。

---

## 5. 冗余代码与删除

- **已删除**  
  - `Source/EqualMilking/Cumpilation/Settings/Cumpilation_SettingsController.cs`：原 Cumpilation 设置 Mod 入口，功能已并入 EqualMilking 的 Tab。  
  - `Source/EqualMilking/Cumpilation/Leaking/Settings/SettingsController.cs`：原泄精设置 Mod 入口，同上。

- **保留但不再承担“设置 UI”的**  
  - `Cumpilation.Settings`、`Cumpilation.Leaking.Settings`：仅保留静态属性代理到 `EqualMilkingSettings`，ExposeData 为空实现；Cumpilation 各处仍读 `Settings.EnableCumflation` 等，无需改调用方。

- **未发现其它明显冗余**  
  - RJW 集成在独立程序集，由 LoadFolders 按 mod 激活加载；Cumpilation 与 EqualMilking 共享主 DLL，无重复定义。若后续发现未使用的 Patch 或 Helper，可再逐项移除。

---

## 文件变更一览

| 变更类型 | 路径 |
|----------|------|
| 新增 | `Source/EqualMilking/UI/Widget_CumpilationSettings.cs` |
| 修改 | `Source/EqualMilking/EqualMilkingSettings.cs`（Cumpilation/Leak 字段 + ExposeData + Tab + case 6） |
| 重写 | `Source/EqualMilking/Cumpilation/Settings/Settings.cs`（代理） |
| 重写 | `Source/EqualMilking/Cumpilation/Leaking/Settings/Settings.cs`（代理） |
| 删除 | `Source/EqualMilking/Cumpilation/Settings/Cumpilation_SettingsController.cs` |
| 删除 | `Source/EqualMilking/Cumpilation/Leaking/Settings/SettingsController.cs` |
| 重命名 | `Defs/HumanMilik.xml` → `Defs/HumanMilk.xml` |
| 修改 | `Defs/UI/PawnColumnDefs.xml`（Milk_Dev label → EM.Milk_Dev） |
| 新增 | `Languages/ChineseSimplified/Keyed/Mod_Settings.xml` |
| 新增 | `Languages/ChineseSimplified/Keyed/UI_Elements.xml` |
| 修改 | `Languages/English/Keyed/lang.xml`（EM.Milk_Dev） |
| 修改 | `Languages/ChineseSimplified/Keyed/lang.xml`（EM.Milk_Dev） |
