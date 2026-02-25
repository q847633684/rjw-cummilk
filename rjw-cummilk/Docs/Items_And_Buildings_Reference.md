# 建筑与物品一览：功能与修改方式

本文列出本 Mod 中与奶/精液相关的**建筑**、**物品**、**污物**与**产出规则**，并说明如何修改。

---

## 一、建筑（ThingDef）

| defName | 中文名 | 功能 | 产出/存储 | 如何改 |
|--------|--------|------|-----------|--------|
| **EM_MilkingPump** | 手动挤奶泵 | 挤奶 + 泄精；**可链接床** | 储格：Milk、EM_HumanMilk、Cumpilation_Cum；**动物奶可进管道** | Defs/BuildingDefs.xml：comps（DeflateBucket、CompCumBucketLink） |
| **EM_MilkingElectric** | 电动挤奶泵 | 同上，需电力；挤奶/泄精效率更高；**可链接床** | 同上；**人奶与精液不进入管道，直接落格** | 同上 + comps 里 DeflateBucket 的 deflateMult/deflateRate |
| **stool_with_cumbucket** | 带精液桶的凳子 | 泄精（坐上去排精）；**可链接床** | Cumpilation_Cum | Defs/ThingDefs/Buildings_Furniture.xml；DeflateBucket、CompCumBucketLink |
| **chair_with_cumbucket** | 带精液桶的椅子 | 同上；**可链接床** | 同上 | 同上 |
| **autochair_with_cumtank** | 带精液罐的自动椅 | 同上，需电力，效率更高；**可链接床** | 同上 | 同上 |

### 电动泵（EM_MilkingElectric）产出一句说明

- **挤奶**：产物为 **Milk**（动物）或 **EM_HumanMilk**（人）。动物奶可被 `CompProperties_ConvertThingToResource` 转为管道资源（EM_MilkNet）；**人奶不进入管道**，直接放在泵格（PipeSystem 的 Patch 控制）。
- **泄精**：产物为 **Cumpilation_Cum**，直接放在泵格（泵不做性交收集）。
- 因此「电动泵收集的物品」= 泵格上可见的 **Milk / EM_HumanMilk / Cumpilation_Cum**；管道里只有 **Milk**（动物奶）。

---

## 二、物品（ThingDef）

| defName | 中文名 | 用途 | 产主规则 | 如何改 |
|--------|--------|------|----------|--------|
| **Milk** | 奶（原版） | 动物奶，可食用/烹饪 | 无 CompShowProducer | 原版/其他 Mod |
| **EM_HumanMilk** | 人奶 | 人挤出的奶，可食用/烹饪 | CompShowProducer.producer = 挤奶者 | 谁可吃：奶表格「指定」→ 谁可吃我的奶制品 |
| **Cumpilation_Cum** | 精液 | 泄精/手术收集，可食用/加工 | CompShowProducer.producer = 产精者（单源时）；混合桶/污物回收无产主 | 谁可吃：奶表格「指定」→ 谁可吃我的精液制品 |
| **InsectJelly** | 虫胶 | 清洁虫胶污物回收产出 | 无产主 | 同原版 |

---

## 三、污物（Filth）与回收

| Filth defName | 来源 | 可被谁回收 | 回收成 | FluidGatheringDef |
|---------------|------|------------|--------|-------------------|
| （Cum 对应污物，RJW 定义） | 泄精/泄漏/性交等 | 清洁 Job、机械体清洁 | Cumpilation_Cum | Cumpilation_Basic_Cum_Gathering |
| **EM_HumanMilkFilth** | 人奶洒地 | 清洁 Job | EM_HumanMilk | EM_HumanMilk_FromFilth |
| （InsectSpunk 污物） | RJW | 清洁等 | InsectJelly | Cumpilation_Basic_InsectSpunk_Gathering |

- **清洁 Job / 机械体清洁**：小人或机械体做清洁时，按 FluidGatheringDef 把污物回收成物品；**不查产主**，产物一律**无产主**（混合/匿名）。

---

## 四、产主规则与“谁的精液就是谁的”

| 来源 | 是否带产主 | 说明 |
|------|-------------|------|
| 手术抽取精液 | ✅ 有 | 产主 = 被抽的小人 |
| 泄精到椅/泵（单主） | ✅ 有 | 产主 = 当前排精者体内 Hediff 的 sources 按权重；链接床时产主=床主 |
| 泄精到椅/泵（多人用过同一建筑） | ❌ 无 | 视为混合精液（仅未链接时会出现多人共用） |
| 挤奶（泵） | ✅ 有 | 产主 = 被挤奶者 |
| **清洁 Job / 机械体清洁地面污物** | ❌ 无 | 不查产主，产物混合/匿名 |

**若要坚持“谁的精液就是谁的”**：

1. **仅用“直接路径”**：泄精到椅/泵、手术抽取，并为椅/泵**链接床**（链接后产主=床主；罐内产主≠床主则无法链接）。泵不做性交收集。
2. 清洁 Job 回收的地面污物无法打产主。

---

## 五、FluidGatheringDef 与修改

| defName | fluidDef | thingDef | 污物回收 | 用途 |
|---------|----------|----------|----------|------|
| Cumpilation_Basic_Cum_Gathering | Cum | Cumpilation_Cum | 是，filthNecessaryForOneUnit=10 | 精液收集/污物回收 |
| Cumpilation_Basic_InsectSpunk_Gathering | InsectSpunk | InsectJelly | 是 | 虫胶 |
| EM_HumanMilk_FromFilth | （无） | EM_HumanMilk | 是，filth=EM_HumanMilkFilth | 人奶污物回收 |

- 文件：`Defs/FluidGatheringDefs/BasicFluidGatheringDefs.xml`、`HumanMilkGathering.xml`。
- 改产出：改 `thingDef`、`fluidRequiredForOneUnit`、`filthNecessaryForOneUnit` 等。

---

## 六、管道（PipeSystem）

- 只有 **Milk**（原版动物奶）会被泵转为管道资源（EM_MilkNet）。
- **EM_HumanMilk** 与 **Cumpilation_Cum** 不进入管道，直接落建筑格。
- 逻辑在 `PipeSystem.cs` 的 `AddResourceConversions` 与 `Building_Milking_PlaceMilkThing_Patch`。

---

## 七、快速对照：想改什么看哪里

| 想改的内容 | 看/改哪里 |
|------------|------------|
| 电动泵/手动泵的挤奶或泄精效率 | BuildingDefs.xml：equippedStatOffsets、DeflateBucket 的 deflateMult/deflateRate |
| 污物回收成什么、多少污物换 1 单位 | FluidGatheringDefs：thingDef、filthNecessaryForOneUnit |
| 谁可吃奶/精液制品 | 奶表格「指定」+ CompShowProducer + allowedConsumers（代码见 ExtensionHelper / CompEquallyMilkable） |
| 管道里走什么 | PipeSystem.cs：AddResourceConversions、PlaceMilkThing_Prefix |
