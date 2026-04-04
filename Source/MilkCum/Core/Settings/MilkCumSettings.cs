using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>乳池单侧基容量如何从 RJW 乳房 Hediff 推导（再乘 <see cref="MilkCumSettings.rjwBreastCapacityCoefficient"/> 的语义见各模式说明）。仅存三档：纯严重度 / 纯重量 / 纯体积。</summary>
public enum RjwBreastPoolCapacityMode : byte
{
    /// <summary>严重度×系数。</summary>
    Severity = 0,
    /// <summary>RJW PartSize 估算乳房重量（kg）×系数；无有效重量数据时为 0（不进池）。</summary>
    RjwBreastWeight = 1,
    /// <summary>RJW <c>BreastSize.volume</c>（升）×系数；无有效体积数据时为 0（不进池）。新安装默认。</summary>
    RjwBreastVolume = 2,
}

/// <summary>RJW 乳房 Hediff 与乳池条目的拓扑：胸位单池 / 虚拟左·右 / 每叶独立池键。</summary>
public enum RjwBreastPoolTopologyMode : byte
{
    /// <summary>挂胸（<c>Chest</c>）或无部位的可泌乳乳房 Hediff 合并为单一虚拟池（键名与 <c>BreastLeft</c> 枚举一致）。</summary>
    RjwChestUnified = 0,
    /// <summary>默认：<c>Breast</c>/<c>MechBreast</c> 叶 + 标签辨左右，聚合成虚拟左/右两槽。</summary>
    VirtualLeftRight = 1,
    /// <summary>每叶一条池，键为 RjwBreastPoolEconomy.MakeStablePoolKey 路径串；条目 Site 为 None。</summary>
    PerAnatomicalLeaf = 2,
}

/// <summary>专业级 UI：按系统类型分层。核心机制 / 健康风险 / 权限规则 / 数值平衡 / 模组联动 / 数据种族 / 调试工具（仅 DevMode）。</summary>
public enum MainTabIndex
{
	CoreSystems = 0,
	HealthRisk = 1,
	Permissions = 2,
	Balance = 3,
	Integrations = 4,
	DataRaces = 5,
	DevTools = 6
}

[StaticConstructorOnStartup]
internal partial class MilkCumSettings : ModSettings
{
	/// <summary>挤奶流速基准：baseFlowPerSecond = 60/基准值（池单位/秒）。默认 60 → 满池约 1 瓶/秒（现实时间）；调大则变慢。</summary>
	public static float milkingWorkTotalBase = 60f;
	public static bool femaleAnimalAdultAlwaysLactating = false;
	public static bool showMechOptions = true;
	public static bool showColonistOptions = true;
	public static bool showSlaveOptions = true;
	public static bool showPrisonerOptions = true;
	public static bool showAnimalOptions = true;
	public static bool showMiscOptions = true;
	/// <summary>默认允许用奶规则（名单为空时）：是否把「子女」算入默认允许。</summary>
	public static bool defaultSucklerIncludeChildren = true;
	/// <summary>默认允许用奶规则：是否把「恋人」算入默认允许。</summary>
	public static bool defaultSucklerIncludeLover = true;
	/// <summary>默认允许用奶规则：是否把「配偶」算入默认允许。</summary>
	public static bool defaultSucklerIncludeSpouse = true;
	/// <summary>默认允许用奶规则：是否排除「父母」（母亲/父亲）。</summary>
	public static bool defaultSucklerExcludeParents = true;
	public static float nutritionToEnergyFactor = 100f;
	/// <summary>泌乳额外营养基准：以 150 为 1:1，将每日泌乳进水量折算成额外营养（用于进食上限校正）。</summary>
	public static int lactationExtraNutritionBasis = 150;
	/// <summary>回缩吸收开关：开启时，池内剩余奶量会按下方效率转成营养；关闭时，未被吸奶消耗的部分全部作废。</summary>
	public static bool reabsorbNutritionEnabled = true;
	/// <summary>回缩吸收效率，0~1：吸收的池单位折成营养的比例，默认 0.5 避免满池挂机过强。</summary>
	public static float reabsorbNutritionEfficiency = 0.5f;
	/// <summary>DevMode 且勾选时，每 60 tick 输出泌乳小人的营养/奶池/回缩/吸奶明细到日志。</summary>
	public static bool lactationPoolTickLog = false;
	/// <summary>DevMode 下输出挤奶扣池明细；不依赖 <see cref="lactationLog"/>。已存档，「调试工具」可勾选。</summary>
	public static bool milkingActionLog = false;
	/// <summary>DevMode 时打开：输出泌乳关键流程日志（进水/衰减/溢出等），关闭可减少刷屏；仅用于细查 PoolTickLog 不方便的情况。</summary>
	public static bool lactationLog = true;
	/// <summary>DevMode 时打开：每次药物带来进水（AddFromDrug）时输出调试日志，包含 ΔS、进水量与剩余时间变化。</summary>
	public static bool lactationDrugIntakeLog = false;
	/// <summary>仅在 DevMode 且 lactationLog 打开时调用：辅助输出与 L/池/污物/衰减表相关的详细日志。</summary>
	public static void LactationLog(string message)
	{
		if (Verse.Prefs.DevMode && lactationLog && !string.IsNullOrEmpty(message))
			Verse.Log.Message("[MilkCum.Lactation] " + message);
	}
	/// <summary>仅当 DevMode 且 lactationPoolTickLog 为 true 时输出，用于每步营养/奶池/回缩/吸奶明细。</summary>
	public static void PoolTickLog(string message)
	{
		if (Verse.Prefs.DevMode && lactationPoolTickLog && !string.IsNullOrEmpty(message))
			Verse.Log.Message("[MilkCum.Pool] " + message);
	}

	/// <summary>仅当 DevMode 且 <see cref="milkingActionLog"/> 为 true 时输出；与 <see cref="lactationLog"/> 无关。</summary>
	public static void MilkingActionLogMessage(string message)
	{
		if (Verse.Prefs.DevMode && milkingActionLog && !string.IsNullOrEmpty(message))
			Verse.Log.Message("[MilkCum.Milking] " + message);
	}
	public static HumanlikeBreastfeed humanlikeBreastfeed = new();
	public static AnimalBreastfeed animalBreastfeed = new();
	public static MechanoidBreastfeed mechanoidBreastfeed = new();
	// 泌乳期意义/操纵/移动收益：开关与百分比(0~0.20 = 0%~20%)
	public static bool lactatingGainEnabled = true;
	public static float lactatingGainCapModPercent = 0.10f;
	/// <summary>RJW 胸围容量系数：单侧基容量 =（由 <see cref="rjwBreastPoolCapacityMode"/> 选定的严重度/体积/重量等基值）× 本系数，默认 2。仅当 RJW（rim.job.world）在 mod 列表中时乳房池才使用 RJW 数据。</summary>
	public static float rjwBreastCapacityCoefficient = 2f;
	/// <summary>乳池单侧基容量来源：默认 <see cref="RjwBreastPoolCapacityMode.RjwBreastVolume"/>（体积/重量无有效 RJW 尺寸则为 0）；流速仍独立走 <c>GetFluidMultiplier</c>。</summary>
	public static RjwBreastPoolCapacityMode rjwBreastPoolCapacityMode = RjwBreastPoolCapacityMode.RjwBreastVolume;
	/// <summary>乳房 Hediff 与虚拟乳池的拓扑；默认 <see cref="RjwBreastPoolTopologyMode.VirtualLeftRight"/>（与历史行为一致）。</summary>
	public static RjwBreastPoolTopologyMode rjwBreastPoolTopologyMode = RjwBreastPoolTopologyMode.VirtualLeftRight;
	/// <summary>RJW 乳房阶段标签含 “Nipple” 时，对进水/高潮产液流速倍率施加的百分比修正（0=关闭）；限制在约 ±15% 内。</summary>
	public static float rjwNippleStageFlowBonusPercent = 0f;
	/// <summary>泌乳期临时体型增益（0~1 段）：RJW Severity 增量，满 L 时生效；默认 0.15。</summary>
	public static float rjwLactatingSeverityBonus = 0.15f;
	/// <summary>池 1→1.2 撑大段对应的 RJW Severity 增量；默认 0.05。</summary>
	public static float rjwLactatingStretchSeverityBonus = 0.05f;
	/// <summary>是否启用「因泌乳永久撑大」：每达到一定泌乳时长即通过 RJW 的 SetSeverity 永久增加乳房体型，不在本 mod 内维护单独数字。</summary>
	public static bool rjwPermanentBreastGainFromLactationEnabled = false;
	/// <summary>每多少游戏日泌乳触发一次永久体型增益（每里程碑对每乳调用 comp.SetSeverity(Min(1f, base + delta))）。</summary>
	public static float rjwPermanentBreastGainDaysPerMilestone = 10f;
	/// <summary>每次里程碑永久增加的 Severity 量（RJW 定义），上限 1。</summary>
	public static float rjwPermanentBreastGainSeverityDelta = 0.03f;
	/// <summary>泌乳期对 RJW 生育能力的倍率（0~1），1 = 不影响，0.85 = 略微降低怀孕率。</summary>
	public static float rjwLactationFertilityFactor = 0.85f;
	/// <summary>性行为结束后泌乳进水强度；RJW 已加载且 &gt;0 时生效。</summary>
	public static float rjwSexLactationBoostDeltaS = 0.15f;
	// 乳腺炎/耐受相关配置已拆分到 MilkCumSettings.Risk.cs。
	// 耐受动态：dE/dt = μ×L − ν×E；启用时由 mod 维护的 E 计算 E_tol（流速/容量），取代仅用游戏内耐受严重度 t。
	public static bool enableToleranceDynamic = true;
	/// <summary>耐受累积率 μ（每游戏日）：越高，长时间高 L 时 E 上升越快。</summary>
	public static float toleranceDynamicMu = 0.03f;
	/// <summary>耐受衰减率 ν（每游戏日）：越高，停药后耐受自然回落越快。</summary>
	public static float toleranceDynamicNu = 0.08f;
	// 满池溢出地面污物：Def 名称，空或无效时回退 Filth_Vomit
	public static string overflowFilthDefName = "Filth_Vomit";
	/// <summary>泌乳水平上限（L_cap）：&gt;0 时，吃药超过此值的部分不再增加进水量 L，只增加 Severity（延长泌乳时间），流速由 min(L, cap) 决定，更符合生理（身体需更长时间消耗药效）。0 = 关闭。</summary>
	public static float lactationLevelCap = 0f;
	/// <summary>上限溢出转时间时的倍数：超出 cap 的 ΔL 转为 Severity 时乘以该系数，使「只延长时间」的收益更大（&gt;1 则延长更多天），补偿不提高流速。默认 1.5。</summary>
	public static float lactationLevelCapDurationMultiplier = 1.5f;
	/// <summary>泌乳药物单次在 XML 中对耐受 Hediff 的 Severity 增量（与 Lactating 一并叠加），默认 0.044；若改 XML 请同步更新此处说明。</summary>
	public static float ProlactinToleranceGainPerDose = 0.044f;
	// 压力/炎症/适应/导管等模型参数与方法已拆分到 MilkCumSettings.Model.cs。
	// AI 挤奶工作：是否优先选择更满的一侧作为目标（低满度的会先去找更满的一侧）。 
	public static bool aiPreferHighFullnessTargets = true;
	// 物种兼容表：允许名单（defName 在此列表中视为始终可泌乳）；禁止名单（defName 在此列表中视为永不泌乳）。
	public static List<string> raceCanAlwaysLactate = new();
	public static List<string> raceCannotLactate = new();
	// 人形种族默认流速倍率：2 = 在标准体型、乳量等级 1 时约等于一次就能挤空；与 RJW/其他泌乳类 mod 联动时也可调。
	public static float defaultFlowMultiplierForHumanlike = 2f;
	// 压力/炎症/组织适应/品质/提醒等模型参数已拆分到 MilkCumSettings.Model.cs。
	// 3.3：按物种配置泌乳药物 ΔS 乘法修正；未配置的种族默认倍率为 1。
	public static List<string> raceDrugDeltaSMultiplierDefNames = new();
	public static List<float> raceDrugDeltaSMultiplierValues = new();
	public static MilkSettings colonistSetting = new();
	public static MilkSettings slaveSetting = new();
	public static MilkSettings prisonerSetting = new();
	public static MilkSettings animalSetting = new();
	public static MilkSettings mechSetting = new();
	public static MilkSettings entitySetting = new();
	public static IEnumerable<ThingDef> pawnDefs;
	public static IEnumerable<ThingDef> productDefs;
	public static Dictionary<ThingDef, RaceMilkType> defaultMilkProducts;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref milkingWorkTotalBase, "EM.MilkingWorkTotalBase", 60f);
		Scribe_Values.Look(ref femaleAnimalAdultAlwaysLactating, "EM.FemaleAnimalAdultAlwaysLactating", false);
		Scribe_Values.Look(ref showMechOptions, "EM.ShowMechOptions", true);
		Scribe_Values.Look(ref showColonistOptions, "EM.ShowColonistOptions", true);
		Scribe_Values.Look(ref showSlaveOptions, "EM.ShowSlaveOptions", true);
		Scribe_Values.Look(ref showPrisonerOptions, "EM.ShowPrisonerOptions", true);
		Scribe_Values.Look(ref showAnimalOptions, "EM.ShowAnimalOptions", true);
		Scribe_Values.Look(ref showMiscOptions, "EM.ShowMiscOptions", true);
		Scribe_Values.Look(ref defaultSucklerIncludeChildren, "EM.DefaultSucklerIncludeChildren", true);
		Scribe_Values.Look(ref defaultSucklerIncludeLover, "EM.DefaultSucklerIncludeLover", true);
		Scribe_Values.Look(ref defaultSucklerIncludeSpouse, "EM.DefaultSucklerIncludeSpouse", true);
		Scribe_Values.Look(ref defaultSucklerExcludeParents, "EM.DefaultSucklerExcludeParents", true);
		Scribe_Values.Look(ref nutritionToEnergyFactor, "EM.NutritionToEnergyFactor", 100f);
		Scribe_Values.Look(ref lactationExtraNutritionBasis, "EM.LactationExtraNutritionFactor", 150);
		Scribe_Values.Look(ref reabsorbNutritionEnabled, "EM.ReabsorbNutritionEnabled", true);
		Scribe_Values.Look(ref reabsorbNutritionEfficiency, "EM.ReabsorbNutritionEfficiency", 0.5f);
		Scribe_Values.Look(ref lactatingGainEnabled, "EM.LactatingGainEnabled", true);
		Scribe_Values.Look(ref lactatingGainCapModPercent, "EM.LactatingGainCapModPercent", 0.10f);
		Scribe_Values.Look(ref rjwBreastCapacityCoefficient, "EM.RjwBreastCapacityCoefficient", 2f);
		Scribe_Values.Look(ref rjwBreastPoolCapacityMode, "EM.RjwBreastPoolCapMode", RjwBreastPoolCapacityMode.RjwBreastVolume);
		rjwBreastPoolCapacityMode = (RjwBreastPoolCapacityMode)Mathf.Clamp((int)rjwBreastPoolCapacityMode, 0, 2);
		Scribe_Values.Look(ref rjwBreastPoolTopologyMode, "EM.RjwBreastPoolTopologyMode", RjwBreastPoolTopologyMode.VirtualLeftRight);
		rjwBreastPoolTopologyMode = (RjwBreastPoolTopologyMode)Mathf.Clamp((int)rjwBreastPoolTopologyMode, 0, 2);
		Scribe_Values.Look(ref rjwNippleStageFlowBonusPercent, "EM.RjwNippleStageFlowBonusPct", 0f);
		Scribe_Values.Look(ref rjwLactatingSeverityBonus, "EM.RjwLactatingSeverityBonus", 0.15f);
		Scribe_Values.Look(ref rjwLactatingStretchSeverityBonus, "EM.RjwLactatingStretchSeverityBonus", 0.05f);
		Scribe_Values.Look(ref rjwPermanentBreastGainFromLactationEnabled, "EM.RjwPermanentBreastGainFromLactationEnabled", false);
		Scribe_Values.Look(ref rjwPermanentBreastGainDaysPerMilestone, "EM.RjwPermanentBreastGainDaysPerMilestone", 10f);
		Scribe_Values.Look(ref rjwPermanentBreastGainSeverityDelta, "EM.RjwPermanentBreastGainSeverityDelta", 0.03f);
		Scribe_Values.Look(ref rjwLactationFertilityFactor, "EM.RjwLactationFertilityFactor", 0.85f);
		Scribe_Values.Look(ref rjwSexLactationBoostDeltaS, "EM.RjwSexLactationBoostDeltaS", 0.15f);
		ExposeRiskData();
		Scribe_Values.Look(ref enableToleranceDynamic, "EM.EnableToleranceDynamic", true);
		Scribe_Values.Look(ref toleranceDynamicMu, "EM.ToleranceDynamicMu", 0.03f);
		Scribe_Values.Look(ref toleranceDynamicNu, "EM.ToleranceDynamicNu", 0.08f);
		Scribe_Values.Look(ref ProlactinToleranceGainPerDose, "EM.ProlactinToleranceGainPerDose", 0.044f);
		Scribe_Values.Look(ref overflowFilthDefName, "EM.OverflowFilthDefName", "Filth_Vomit");
		Scribe_Values.Look(ref lactationLevelCap, "EM.LactationLevelCap", 0f);
		Scribe_Values.Look(ref lactationLevelCapDurationMultiplier, "EM.LactationLevelCapDurationMultiplier", 1.5f);
		Scribe_Values.Look(ref aiPreferHighFullnessTargets, "EM.AiPreferHighFullnessTargets", true);
		Scribe_Collections.Look(ref raceCanAlwaysLactate, "EM.RaceCanAlwaysLactate", LookMode.Value);
		Scribe_Collections.Look(ref raceCannotLactate, "EM.RaceCannotLactate", LookMode.Value);
		Scribe_Values.Look(ref defaultFlowMultiplierForHumanlike, "EM.DefaultFlowMultiplierForHumanlike", 2f);
		ExposeModelData();
		Scribe_Collections.Look(ref raceDrugDeltaSMultiplierDefNames, "EM.RaceDrugDeltaSMultiplierDefNames", LookMode.Value);
		Scribe_Collections.Look(ref raceDrugDeltaSMultiplierValues, "EM.RaceDrugDeltaSMultiplierValues", LookMode.Value);
		Scribe_Values.Look(ref lactationPoolTickLog, "EM.LactationPoolTickLog", false);
		Scribe_Values.Look(ref milkingActionLog, "EM.MilkingActionLog", false);
		Scribe_Values.Look(ref lactationLog, "EM.LactationLog", true);
		Scribe_Values.Look(ref lactationDrugIntakeLog, "EM.LactationDrugIntakeLog", false);
		ExposeCumData();
		Scribe_Deep.Look(ref humanlikeBreastfeed, "EM.HumanlikeBreastfeed");
		Scribe_Deep.Look(ref animalBreastfeed, "EM.AnimalBreastfeed");
		Scribe_Deep.Look(ref mechanoidBreastfeed, "EM.MechanoidBreastfeed");
		ExposeDataMappings();
		Scribe_Deep.Look(ref colonistSetting, "EM.ColonistSetting");
		Scribe_Deep.Look(ref slaveSetting, "EM.SlaveSetting");
		Scribe_Deep.Look(ref prisonerSetting, "EM.PrisonerSetting");
		Scribe_Deep.Look(ref animalSetting, "EM.AnimalSetting");
		Scribe_Deep.Look(ref mechSetting, "EM.MechSetting");
		Scribe_Deep.Look(ref entitySetting, "EM.EntitySetting");
		ExposeRealismData();
	}

	internal void UpdateMilkCumSettings()
	{
		EventHelper.TriggerSettingsChanged();
		pawnDefs = GetMilkablePawns();
		defaultMilkProducts = GetDefaultMilkProducts();
		if (HediffDefOf.Lactating != null)
		{
			HediffDefOf.Lactating.maxSeverity = 100f; // 允许 severity 按公式自由提高到 100。
		}
		else
		{
			Log.Warning("[MilkCum] HediffDefOf.Lactating is null, skip maxSeverity patch.");
		}
		foreach (ThingDef pawnDef in pawnDefs)
		{
			UpdateEqualMilkableComp(pawnDef);
		}
		productDefs = GetProductDefs();
		foreach (ThingDef milkDef in productDefs)
		{
			AddOrSetShowProducerComp(milkDef);
		}
		// Recipe products from human milk (e.g. vanilla Milk) can show producer
		ThingDef vanillaMilk = DefDatabase<ThingDef>.GetNamedSilentFail("Milk");
		if (vanillaMilk != null)
		{
			AddOrSetShowProducerComp(vanillaMilk);
			if (!productsToTags.ContainsKey("Milk"))
				productsToTags.Add("Milk", new MilkTag("Milk", true, false));
		}
		// 人奶：默认开启「显示生产者」，产线有上限，可在标签里手动关闭。
		ThingDef humanMilkDef = DefDatabase<ThingDef>.GetNamedSilentFail("EM_HumanMilk");
		if (humanMilkDef != null && !productsToTags.ContainsKey("EM_HumanMilk"))
			productsToTags.Add("EM_HumanMilk", new MilkTag("EM_HumanMilk", true, false));
		// 7.10: rjw-genes cum milk etc. — ensure breast-sourced cum gets producer so allowedConsumers apply
		ThingDef cumDef = DefDatabase<ThingDef>.GetNamedSilentFail("Cum_Cum");
		if (cumDef != null && !productsToTags.ContainsKey("Cum_Cum"))
			productsToTags.Add("Cum_Cum", new MilkTag("Cum_Cum", true, false));
		foreach (Pawn pawn in PawnsFinder.AllMaps)
		{
			pawn.LactatingHediffWithComps()?.SetDirty();
		}
	}
}
