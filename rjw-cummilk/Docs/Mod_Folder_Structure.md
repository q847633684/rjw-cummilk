# 模组目录结构说明

## RimWorld 常见两种做法

### 1. 扁平结构（单版本 / 最常见）

内容**全部在模组根目录**，没有 `Versions` 或版本子目录：

```
3266052474/
├── About/
├── Assemblies/
├── Defs/
├── Languages/
├── Patches/
├── Sounds/
├── Textures/
└── LoadFolders.xml   （可选；若用则只列根或条件子目录）
```

- 游戏会加载根目录下的 Defs、Languages、Patches 等。
- 若使用 LoadFolders，通常用 `<li></li>` 或 `<li>/</li>` 表示加载根目录；条件加载可写 `Mods/Biotech`、`Integrations/RJW` 等（相对根目录）。

### 2. 版本化结构（多版本支持）

内容按版本放在子目录，用 LoadFolders 指定加载哪些：

```
3266052474/
├── About/
├── Versions/
│   └── 1.6/
│       ├── Assemblies/
│       ├── Defs/
│       ├── Languages/
│       ├── Patches/
│       ├── Mods/
│       └── Integrations/
└── LoadFolders.xml   （列出 Versions/1.6 及条件子目录）
```

- LoadFolders 里**只**列 `Versions/1.6` 和条件路径（如 `Versions/1.6/Mods/Biotech`），**不**列根目录的 Defs/Languages（根目录就不放这些内容）。
- 这样只有一套 Defs、一套 Languages，不会出现“根目录一份 + Versions/1.6 一份”的重复。

---

## 当前本 Mod 的情况

- **根目录**：有 Defs、Languages、Patches、Textures、About 等。
- **Versions/1.6**：也有 Defs、Languages、Patches，以及 Mods、Integrations、Assemblies、CumpilationCommon。
- **LoadFolders**：`<li></li>`（加载根）+ `<li>Versions/1.6</li>`（再加载 1.6），所以**两套都会被加载**。

结果就是：存在两处 Defs（`3266052474\Defs` 和 `3266052474\Versions\1.6\Defs`）、两处 Languages 等，容易混淆，也不是“只选一种”的常规做法。

---

## 建议：统一成一种

任选其一即可。

### 方案 A：统一到根目录（推荐，和多数单版本 mod 一致）

- 把 `Versions/1.6` 下所有内容**合并到根目录**：
  - `Versions/1.6/Defs/*` → 合并到根 `Defs/`
  - `Versions/1.6/Languages/*` → 合并到根 `Languages/`
  - `Versions/1.6/Patches/*` → 合并到根 `Patches/`
  - `Versions/1.6/Assemblies` → 根 `Assemblies/`
  - `Versions/1.6/Mods` → 根 `Mods/`
  - `Versions/1.6/Integrations` → 根 `Integrations/`
  - `Versions/1.6/CumpilationCommon` → 视需要合并到根（例如 Textures）
- LoadFolders 改为只加载根 + 条件子目录，例如：
  - `<li></li>`
  - `<li IfModActive="Ludeon.RimWorld.Biotech">Mods/Biotech</li>`
  - … 其他条件路径相对根目录
- 之后可删除空的 `Versions/1.6`（或保留空目录仅作占位）。

这样就只有：`3266052474\Defs`、`3266052474\Languages`、`3266052474\Patches` 等，没有 `Versions\1.6\Defs` 等重复。

### 方案 B：统一到 Versions/1.6

- 把**根目录**的 Defs、Languages、Patches 等**移入** `Versions/1.6` 对应子目录并合并。
- LoadFolders 里**去掉** `<li></li>`，只保留：
  - `<li>Versions/1.6</li>`
  - 以及 `Versions/1.6/Mods/Biotech` 等条件项。
- 根目录只保留 About、Textures、Source、Docs、LoadFolders 等非游戏加载内容。

这样所有“游戏会加载”的内容都在 `Versions/1.6` 下，根目录不再有 Defs/Languages/Patches。

---

**总结**：正常模组要么“全部在根”，要么“全部在版本目录”，不会两处各放一套 Defs/Languages。你当前是两处都有；建议按上面 A 或 B 选一种统一，目录就清晰了。

---

## 已执行：方案 A（统一到根目录）

- 已将 `Versions/1.6` 下 Defs、Languages、Patches、Assemblies、Mods、Integrations、CumpilationCommon 全部合并到模组根目录。
- `LoadFolders.xml` 已改为仅加载根目录：`<li></li>` + 条件路径 `Mods/Biotech`、`Integrations/RJW` 等（相对根目录）。
- 已删除 `Versions/1.6`；若 `Versions` 为空也已删除。
- 当前仅有一套 Defs、Languages、Patches 等，位于根目录。
