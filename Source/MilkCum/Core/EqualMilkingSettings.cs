using System.Collections.Generic;
using System.Linq;
using MilkCum.Milk.Comps;
using MilkCum.Milk.Helpers;
using MilkCum.UI;
using MilkCum.Milk.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Core;
[StaticConstructorOnStartup]
internal class EqualMilkingSettings : ModSettings
{
	private static Dictionary<string, RaceMilkType> namesToProducts = new();
	private static Dictionary<string, MilkTag> productsToTags = new();
	public static int maxLactationStacks = 5;
	public static float lactatingEfficiencyMultiplierPerStack = 1.25f;
	public static float milkAmountMultiplierPerStack = 1f;
	public static float hungerRateMultiplierPerStack = 1.31f;
	public static float breastfeedTime = 5000f;
	public static bool femaleAnimalAdultAlwaysLactating = false;
	public static bool showMechOptions = true;
	public static bool showColonistOptions = true;
	public static bool showSlaveOptions = true;
	public static bool showPrisonerOptions = true;
	public static bool showAnimalOptions = true;
	public static bool showMiscOptions = true;
	public static float nutritionToEnergyFactor = 100f;
	public static HumanlikeBreastfeed humanlikeBreastfeed = new();
	public static AnimalBreastfeed animalBreastfeed = new();
	public static MechanoidBreastfeed mechanoidBreastfeed = new();
	// 泌乳期意识/操纵/移动增益：开关与百分比 (0~0.20 = 0%~20%)
	public static bool lactatingGainEnabled = true;
	public static float lactatingGainCapModPercent = 0.10f;
	// 谁可以吸奶：无限制 / 同派系 / 仅殖民地 / 仅爱人或亲属
	public static int suckleRestrictionMode = 0; // 0=None, 1=SameFaction, 2=ColonyOnly, 3=LoversOrFamilyOnly
	// 谁可以使用产出的奶/奶制品食物：同上 0~3，4=与“谁可以吸奶”相同
	public static int milkProductConsumptionRestrictionMode = 0; // 4=UseSuckleSetting
	// RJW 联动（仅当 rim.job.world 激活时生效）
	public static bool rjwBreastSizeEnabled = true;
	public static bool rjwLustFromNursingEnabled = true;
	public static bool rjwSexNeedLactatingBonusEnabled = true;
	public static bool rjwSexSatisfactionAfterNursingEnabled = true;
	public static float rjwLactationFertilityFactor = 0.85f; // 泌乳期怀孕概率乘数 (0~1)
	public static bool rjwLactatingInSexDescriptionEnabled = true;
	/// <summary>3.2：性行为后为泌乳参与者增加少量池进水（ΔL），可选。</summary>
	public static bool rjwSexAddsLactationBoost = false;
	public static float rjwSexLactationBoostDeltaS = 0.15f;
	// 乳腺炎/堵塞：卫生触发是否与 Dubs Bad Hygiene 联动（有 DBH 时用 Hygiene 需求，否则用房间清洁度）
	public static bool useDubsBadHygieneForMastitis = true;
	// 乳腺炎可配置：是否启用、基准 MTB（天）、满池过久风险系数、卫生风险系数
	// 耐受对泌乳效率的影响：关闭则 E_tol 恒为 1；指数控制曲线（1=线性）
	// 建议 13：收拢为 MilkRiskSettings，便于序列化与 UI 分组；对外仍用静态属性，存档兼容旧 key
	private static MilkRiskSettings _risk = new MilkRiskSettings();
	private static MilkRiskSettings Risk => _risk ??= new MilkRiskSettings();
	public static bool allowMastitis { get => Risk.allowMastitis; set => Risk.allowMastitis = value; }
	public static float mastitisBaseMtbDays { get => Risk.mastitisBaseMtbDays; set => Risk.mastitisBaseMtbDays = value; }
	public static float overFullnessRiskMultiplier { get => Risk.overFullnessRiskMultiplier; set => Risk.overFullnessRiskMultiplier = value; }
	public static float hygieneRiskMultiplier { get => Risk.hygieneRiskMultiplier; set => Risk.hygieneRiskMultiplier = value; }
	public static bool allowToleranceAffectMilk { get => Risk.allowToleranceAffectMilk; set => Risk.allowToleranceAffectMilk = value; }
	public static float toleranceFlowImpactExponent { get => Risk.toleranceFlowImpactExponent; set => Risk.toleranceFlowImpactExponent = value; }
	public static float mastitisMtbDaysMultiplierHumanlike { get => Risk.mastitisMtbDaysMultiplierHumanlike; set => Risk.mastitisMtbDaysMultiplierHumanlike = value; }
	public static float mastitisMtbDaysMultiplierAnimal { get => Risk.mastitisMtbDaysMultiplierAnimal; set => Risk.mastitisMtbDaysMultiplierAnimal = value; }
	// 满池溢出地面污物：Def 名称，空或无效时回退 Filth_Vomit
	public static string overflowFilthDefName = "Filth_Vomit";
	// 基准泌乳持续天数（参考/显示，对应池模型 B_T / B_T_birth）
	public static float baselineMilkDurationDays = 3f;
	public static float birthInducedMilkDurationDays = 10f;
	// 挤奶工作：是否优先选择满度更高的目标（殖民者会先挤更满的）
	public static bool aiPreferHighFullnessTargets = true;
	// 种族覆盖：白名单（defName 在此列表中视为可产奶）、黑名单（defName 在此列表中视为不可产奶）
	public static List<string> raceCanAlwaysLactate = new();
	public static List<string> raceCannotLactate = new();
	// 人形种族默认流速倍率（1 = 不变，用于与 RJW/Race mod 平衡）
	public static float defaultFlowMultiplierForHumanlike = 1f;
	// 3.3 满池事件：满池过久（约 1 天）时是否发信提醒
	public static bool enableFullPoolLetter = true;
	// 3.3 动物差异化：种族 defName 对应药物进水倍率（未列出的种族为 1）。与参数联动表一致。
	public static List<string> raceDrugDeltaSMultiplierDefNames = new();
	public static List<float> raceDrugDeltaSMultiplierValues = new();
	// Cumpilation（统一到本设置，不再使用单独 Mod 入口）
	public static bool Cumpilation_EnableCumflation = true;
	public static float Cumpilation_GlobalCumflationModifier = 1.0f;
	public static bool Cumpilation_EnableStuffing = true;
	public static float Cumpilation_GlobalStuffingModifier = 1.0f;
	public static bool Cumpilation_EnableBukkake = true;
	public static float Cumpilation_GlobalBukkakeModifier = 1.0f;
	public static bool Cumpilation_EnableFluidGatheringWhileCleaning = true;
	public static float Cumpilation_MaxGatheringCheckDistance = 15.0f;
	public static bool Cumpilation_EnableProgressingConsumptionThoughts = true;
	public static bool Cumpilation_EnableOscillationMechanics = true;
	public static bool Cumpilation_EnableOscillationMechanicsForAnimals = false;
	public static bool Cumpilation_EnableDebugLogging = false;
	public static bool CumpilationLeak_EnableFilthGeneration = true;
	public static bool CumpilationLeak_EnableAutoDeflateBucket = false;
	public static bool CumpilationLeak_EnableAutoDeflateClean = false;
	public static bool CumpilationLeak_EnableAutoDeflateDirty = false;
	public static bool CumpilationLeak_EnablePrivacy = true;
	public static float CumpilationLeak_AutoDeflateMinSeverity = 0.4f;
	public static float CumpilationLeak_AutoDeflateMaxDistance = 100f;
	public static float CumpilationLeak_LeakMult = 5.0f;
	public static float CumpilationLeak_LeakRate = 1.0f;
	public static float CumpilationLeak_DeflateMult = 5.0f;
	public static float CumpilationLeak_DeflateRate = 1.0f;
	public static List<Gene_MilkTypeData> genes = new();
	public static MilkSettings colonistSetting = new();
	public static MilkSettings slaveSetting = new();
	public static MilkSettings prisonerSetting = new();
	public static MilkSettings animalSetting = new();
	public static MilkSettings mechSetting = new();
	public static MilkSettings entitySetting = new();
	private int tabIndex = 0;
	private static readonly float unitSize = 32f;
	public static IEnumerable<ThingDef> pawnDefs;
	public static IEnumerable<ThingDef> productDefs;
	public static Dictionary<ThingDef, RaceMilkType> defaultMilkProducts;
	private static Widget_MilkableTable milkableTable;
	private static Widget_MilkTagsTable milkTagsTable;
	private static Widget_AdvancedSettings advancedSettings;
	private static Widget_BreastfeedSettings breastfeedSettings;
	private static Widget_GeneSetting geneSetting;
	private static Widget_DefaultSetting defaultSettingWidget;
	private static Widget_CumpilationSettings cumpilationSettings;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref maxLactationStacks, "EM.MaxLactationStacks", 5);
		Scribe_Values.Look(ref lactatingEfficiencyMultiplierPerStack, "EM.LactatingEfficiencyMultiplierPerStack", 1.25f);
		Scribe_Values.Look(ref milkAmountMultiplierPerStack, "EM.MilkAmountMultiplierPerStack", 1f);
		Scribe_Values.Look(ref hungerRateMultiplierPerStack, "EM.HungerRateMultiplierPerStack", 1.31f);
		Scribe_Values.Look(ref breastfeedTime, "EM.BreastfeedTime", 5000f);
		Scribe_Values.Look(ref femaleAnimalAdultAlwaysLactating, "EM.FemaleAnimalAdultAlwaysLactating", false);
		Scribe_Values.Look(ref showMechOptions, "EM.ShowMechOptions", true);
		Scribe_Values.Look(ref showColonistOptions, "EM.ShowColonistOptions", true);
		Scribe_Values.Look(ref showSlaveOptions, "EM.ShowSlaveOptions", true);
		Scribe_Values.Look(ref showPrisonerOptions, "EM.ShowPrisonerOptions", true);
		Scribe_Values.Look(ref showAnimalOptions, "EM.ShowAnimalOptions", true);
		Scribe_Values.Look(ref showMiscOptions, "EM.ShowMiscOptions", true);
		Scribe_Values.Look(ref nutritionToEnergyFactor, "EM.NutritionToEnergyFactor", 100f);
		Scribe_Values.Look(ref lactatingGainEnabled, "EM.LactatingGainEnabled", true);
		Scribe_Values.Look(ref lactatingGainCapModPercent, "EM.LactatingGainCapModPercent", 0.10f);
		Scribe_Values.Look(ref suckleRestrictionMode, "EM.SuckleRestrictionMode", 0);
		Scribe_Values.Look(ref milkProductConsumptionRestrictionMode, "EM.MilkProductConsumptionRestrictionMode", 0);
		Scribe_Values.Look(ref rjwBreastSizeEnabled, "EM.RjwBreastSizeEnabled", true);
		Scribe_Values.Look(ref rjwLustFromNursingEnabled, "EM.RjwLustFromNursingEnabled", true);
		Scribe_Values.Look(ref rjwSexNeedLactatingBonusEnabled, "EM.RjwSexNeedLactatingBonusEnabled", true);
		Scribe_Values.Look(ref rjwSexSatisfactionAfterNursingEnabled, "EM.RjwSexSatisfactionAfterNursingEnabled", true);
		Scribe_Values.Look(ref rjwLactationFertilityFactor, "EM.RjwLactationFertilityFactor", 0.85f);
		Scribe_Values.Look(ref rjwLactatingInSexDescriptionEnabled, "EM.RjwLactatingInSexDescriptionEnabled", true);
		Scribe_Values.Look(ref rjwSexAddsLactationBoost, "EM.RjwSexAddsLactationBoost", false);
		Scribe_Values.Look(ref rjwSexLactationBoostDeltaS, "EM.RjwSexLactationBoostDeltaS", 0.15f);
		Scribe_Values.Look(ref useDubsBadHygieneForMastitis, "EM.UseDubsBadHygieneForMastitis", true);
		Scribe_Deep.Look(ref _risk, "EM.MilkRiskSettings");
		if (_risk == null) _risk = new MilkRiskSettings();
		Scribe_Values.Look(ref _risk.allowMastitis, "EM.AllowMastitis", true);
		Scribe_Values.Look(ref _risk.mastitisBaseMtbDays, "EM.MastitisBaseMtbDays", 1.5f);
		Scribe_Values.Look(ref _risk.overFullnessRiskMultiplier, "EM.OverFullnessRiskMultiplier", 1.5f);
		Scribe_Values.Look(ref _risk.hygieneRiskMultiplier, "EM.HygieneRiskMultiplier", 1f);
		Scribe_Values.Look(ref _risk.allowToleranceAffectMilk, "EM.AllowToleranceAffectMilk", true);
		Scribe_Values.Look(ref _risk.toleranceFlowImpactExponent, "EM.ToleranceFlowImpactExponent", 1f);
		Scribe_Values.Look(ref _risk.mastitisMtbDaysMultiplierHumanlike, "EM.MastitisMtbDaysMultiplierHumanlike", 1f);
		Scribe_Values.Look(ref _risk.mastitisMtbDaysMultiplierAnimal, "EM.MastitisMtbDaysMultiplierAnimal", 1f);
		Scribe_Values.Look(ref overflowFilthDefName, "EM.OverflowFilthDefName", "Filth_Vomit");
		Scribe_Values.Look(ref baselineMilkDurationDays, "EM.BaselineMilkDurationDays", 3f);
		Scribe_Values.Look(ref birthInducedMilkDurationDays, "EM.BirthInducedMilkDurationDays", 10f);
		Scribe_Values.Look(ref aiPreferHighFullnessTargets, "EM.AiPreferHighFullnessTargets", true);
		Scribe_Collections.Look(ref raceCanAlwaysLactate, "EM.RaceCanAlwaysLactate", LookMode.Value);
		Scribe_Collections.Look(ref raceCannotLactate, "EM.RaceCannotLactate", LookMode.Value);
		Scribe_Values.Look(ref defaultFlowMultiplierForHumanlike, "EM.DefaultFlowMultiplierForHumanlike", 1f);
		Scribe_Values.Look(ref enableFullPoolLetter, "EM.EnableFullPoolLetter", true);
		Scribe_Collections.Look(ref raceDrugDeltaSMultiplierDefNames, "EM.RaceDrugDeltaSMultiplierDefNames", LookMode.Value);
		Scribe_Collections.Look(ref raceDrugDeltaSMultiplierValues, "EM.RaceDrugDeltaSMultiplierValues", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			raceDrugDeltaSMultiplierDefNames ??= new List<string>();
			raceDrugDeltaSMultiplierValues ??= new List<float>();
		}
		Scribe_Values.Look(ref Cumpilation_EnableCumflation, "Cumpilation.EnableCumflation", true);
		Scribe_Values.Look(ref Cumpilation_GlobalCumflationModifier, "Cumpilation.GlobalCumflationModifier", 1.0f);
		Scribe_Values.Look(ref Cumpilation_EnableStuffing, "Cumpilation.EnableStuffing", true);
		Scribe_Values.Look(ref Cumpilation_GlobalStuffingModifier, "Cumpilation.GlobalStuffingModifier", 1.0f);
		Scribe_Values.Look(ref Cumpilation_EnableBukkake, "Cumpilation.EnableBukkake", true);
		Scribe_Values.Look(ref Cumpilation_GlobalBukkakeModifier, "Cumpilation.GlobalBukkakeModifier", 1.0f);
		Scribe_Values.Look(ref Cumpilation_EnableFluidGatheringWhileCleaning, "Cumpilation.EnableFluidGatheringWhileCleaning", true);
		Scribe_Values.Look(ref Cumpilation_MaxGatheringCheckDistance, "Cumpilation.MaxGatheringCheckDistance", 15.0f);
		Scribe_Values.Look(ref Cumpilation_EnableProgressingConsumptionThoughts, "Cumpilation.EnableProgressingConsumptionThoughts", true);
		Scribe_Values.Look(ref Cumpilation_EnableOscillationMechanics, "Cumpilation.EnableOscillationMechanics", true);
		Scribe_Values.Look(ref Cumpilation_EnableOscillationMechanicsForAnimals, "Cumpilation.EnableOscillationMechanicsForAnimals", false);
		Scribe_Values.Look(ref Cumpilation_EnableDebugLogging, "Cumpilation.EnableDebugLogging", false);
		Scribe_Values.Look(ref CumpilationLeak_EnableFilthGeneration, "CumpilationLeak.EnableFilthGeneration", true);
		Scribe_Values.Look(ref CumpilationLeak_EnableAutoDeflateBucket, "CumpilationLeak.EnableAutoDeflateBucket", false);
		Scribe_Values.Look(ref CumpilationLeak_EnableAutoDeflateClean, "CumpilationLeak.EnableAutoDeflateClean", false);
		Scribe_Values.Look(ref CumpilationLeak_EnableAutoDeflateDirty, "CumpilationLeak.EnableAutoDeflateDirty", false);
		Scribe_Values.Look(ref CumpilationLeak_EnablePrivacy, "CumpilationLeak.EnablePrivacy", true);
		Scribe_Values.Look(ref CumpilationLeak_AutoDeflateMinSeverity, "CumpilationLeak.AutoDeflateMinSeverity", 0.4f);
		Scribe_Values.Look(ref CumpilationLeak_AutoDeflateMaxDistance, "CumpilationLeak.AutoDeflateMaxDistance", 100f);
		Scribe_Values.Look(ref CumpilationLeak_LeakMult, "CumpilationLeak.LeakMult", 5.0f);
		Scribe_Values.Look(ref CumpilationLeak_LeakRate, "CumpilationLeak.LeakRate", 1.0f);
		Scribe_Values.Look(ref CumpilationLeak_DeflateMult, "CumpilationLeak.DeflateMult", 5.0f);
		Scribe_Values.Look(ref CumpilationLeak_DeflateRate, "CumpilationLeak.DeflateRate", 1.0f);
		Scribe_Deep.Look(ref humanlikeBreastfeed, "EM.HumanlikeBreastfeed");
		Scribe_Deep.Look(ref animalBreastfeed, "EM.AnimalBreastfeed");
		Scribe_Deep.Look(ref mechanoidBreastfeed, "EM.MechanoidBreastfeed");
		Scribe_Collections.Look(ref genes, "EM.Genes", LookMode.Deep);
		Scribe_Collections.Look(ref namesToProducts, "EM.NamesToProducts", LookMode.Value, LookMode.Deep);
		Scribe_Collections.Look(ref productsToTags, "EM.ProductsToTags", LookMode.Value, LookMode.Deep);
		Scribe_Deep.Look(ref colonistSetting, "EM.ColonistSetting");
		Scribe_Deep.Look(ref slaveSetting, "EM.SlaveSetting");
		Scribe_Deep.Look(ref prisonerSetting, "EM.PrisonerSetting");
		Scribe_Deep.Look(ref animalSetting, "EM.AnimalSetting");
		Scribe_Deep.Look(ref mechSetting, "EM.MechSetting");
		Scribe_Deep.Look(ref entitySetting, "EM.EntitySetting");
		genes ??= new List<Gene_MilkTypeData>();
		namesToProducts ??= new Dictionary<string, RaceMilkType>();
		raceCanAlwaysLactate ??= new List<string>();
		raceCannotLactate ??= new List<string>();
		// Initialize widgets
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			pawnDefs ??= GetMilkablePawns();
			productDefs ??= GetProductDefs();
			humanlikeBreastfeed ??= new HumanlikeBreastfeed();
			animalBreastfeed ??= new AnimalBreastfeed();
			mechanoidBreastfeed ??= new MechanoidBreastfeed();
			milkableTable = new Widget_MilkableTable(namesToProducts);
			milkTagsTable = new Widget_MilkTagsTable(namesToProducts, productsToTags);
			advancedSettings = new Widget_AdvancedSettings();
			breastfeedSettings = new Widget_BreastfeedSettings(humanlikeBreastfeed, animalBreastfeed, mechanoidBreastfeed);
			geneSetting = new Widget_GeneSetting(genes);
			colonistSetting ??= new MilkSettings();
			slaveSetting ??= new MilkSettings();
			prisonerSetting ??= new MilkSettings();
			animalSetting ??= new MilkSettings();
			mechSetting ??= new MilkSettings();
			entitySetting ??= new MilkSettings();
		}
	}
	public void DoWindowContents(Rect inRect)
	{
		inRect.yMin += unitSize;
		List<TabRecord> tabs = new()
		{
			new(Lang.Pawn, () => tabIndex = 0, tabIndex == 0),
			new(Lang.Join(Lang.Rename, Lang.MilkType), () => tabIndex = 1, tabIndex == 1),
			new(Lang.Advanced, () => tabIndex = 2, tabIndex == 2),
			new(Lang.Breastfeed, () => tabIndex = 3, tabIndex == 3),
			new(Lang.Join(Lang.MilkType, Lang.Genes), () => tabIndex = 4, tabIndex == 4),
			new(Lang.Default, () => tabIndex = 5, tabIndex == 5),
			new("cumpilation_settings_menuname".Translate(), () => tabIndex = 6, tabIndex == 6)
		};
		TabDrawer.DrawTabs(inRect, tabs);
		Widgets.DrawMenuSection(inRect);
		Rect contentRect = inRect.ContractedBy(unitSize / 2);
		switch (tabIndex)
		{
			case 0:
				milkableTable ??= new Widget_MilkableTable(namesToProducts);
				milkableTable.Draw(contentRect);
				break;
			case 1:
				milkTagsTable ??= new Widget_MilkTagsTable(namesToProducts, productsToTags);
				milkTagsTable.Draw(contentRect);
				break;
			case 2:
				advancedSettings ??= new Widget_AdvancedSettings();
				advancedSettings.Draw(contentRect);
				break;
			case 3:
				breastfeedSettings ??= new Widget_BreastfeedSettings(humanlikeBreastfeed, animalBreastfeed, mechanoidBreastfeed);
				breastfeedSettings.Draw(contentRect);
				break;
			case 4:
				geneSetting ??= new Widget_GeneSetting(genes);
				geneSetting.Draw(contentRect);
				break;
			case 5:
				defaultSettingWidget ??= new Widget_DefaultSetting(
					colonistSetting,
					slaveSetting,
					prisonerSetting,
					animalSetting,
					mechSetting,
					entitySetting);
				defaultSettingWidget.Draw(contentRect);
				break;
			case 6:
				cumpilationSettings ??= new Widget_CumpilationSettings();
				cumpilationSettings.Draw(contentRect);
				break;
		}
	}
	private static IEnumerable<ThingDef> GetMilkablePawns()
	{
		IEnumerable<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefs.Where(x => x.race != null && !x.IsCorpse)
			.OrderByDescending(def => def.race.Humanlike)
			.ThenByDescending(def => def.race.Animal)
			.ThenByDescending(def => def.race.IsMechanoid)
			.ThenBy(def => def.race.Insect)
			.ThenByDescending(def => def.modContentPack?.IsOfficialMod == true)
			.ThenBy(def => def.modContentPack?.Name ?? "")
			.ThenBy(def => def.defName);
		return allDefs;
	}
	private static IEnumerable<ThingDef> GetProductDefs()
	{
		return namesToProducts.Values.Where(product => product?.milkTypeDefName != null && DefDatabase<ThingDef>.GetNamedSilentFail(product.milkTypeDefName) != null)
									.Select(product => DefDatabase<ThingDef>.GetNamedSilentFail(product.milkTypeDefName)).Distinct();
	}
	private Dictionary<ThingDef, RaceMilkType> GetDefaultMilkProducts()
	{
		Dictionary<ThingDef, RaceMilkType> milkProducts = new();
		foreach (ThingDef def in pawnDefs)
		{
			RaceMilkType milkProduct = new();
			CompProperties_Milkable compMilkable = def.GetCompProperties<CompProperties_Milkable>();
			if (compMilkable?.milkDef != null)
			{
				milkProduct.milkTypeDefName = compMilkable.milkDef.defName;
				milkProduct.milkAmount = compMilkable.milkAmount;
				milkProduct.milkIntervalDays = compMilkable.milkIntervalDays;
				milkProduct.isMilkable = true;
				milkProducts.Add(def, milkProduct);
			}
		}
		return milkProducts;
	}
	internal void UpdateEqualMilkingSettings()
	{
		// 7.11: 旧存档兼容 — 补全缺失的 allowedSucklers/allowedConsumers 并清理无效引用
		foreach (Pawn p in PawnsFinder.AllMaps)
		{
			if (p?.CompEquallyMilkable() is CompEquallyMilkable comp)
				comp.EnsureSaveCompatAllowedLists();
		}
		EventHelper.TriggerSettingsChanged();
		pawnDefs ??= GetMilkablePawns();
		defaultMilkProducts ??= GetDefaultMilkProducts();
		HediffDefOf.Lactating.maxSeverity = 100f; // 不再用 maxLactationStacks 限制，允许 severity 自由叠加
		humanlikeBreastfeed ??= new HumanlikeBreastfeed();
		animalBreastfeed ??= new AnimalBreastfeed();
		mechanoidBreastfeed ??= new MechanoidBreastfeed();
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
		// 7.10: rjw-genes cum milk etc. — ensure breast-sourced cum gets producer so allowedConsumers apply
		ThingDef cumDef = DefDatabase<ThingDef>.GetNamedSilentFail("Cumpilation_Cum");
		if (cumDef != null && !productsToTags.ContainsKey("Cumpilation_Cum"))
			productsToTags.Add("Cumpilation_Cum", new MilkTag("Cumpilation_Cum", true, false));
		foreach (Pawn pawn in PawnsFinder.AllMaps)
		{
			pawn.LactatingHediffWithComps()?.SetDirty();
		}
	}
	private void UpdateEqualMilkableComp(ThingDef pawnDef)
	{
		CompProperties_Milkable compProperties = pawnDef.GetCompProperties<CompProperties_Milkable>();
		if (compProperties == null)
		{
			compProperties = new CompProperties_Milkable();
			pawnDef.comps.Add(compProperties);
		}
		if (!namesToProducts.ContainsKey(pawnDef.defName))
		{
			namesToProducts.Add(pawnDef.defName, GetDefaultMilkProduct(pawnDef));
		}
		compProperties.Set(namesToProducts[pawnDef.defName]);
	}

	internal static RaceMilkType GetDefaultMilkProduct(ThingDef def)
	{
		RaceMilkType milkProduct = new();
		if (defaultMilkProducts.ContainsKey(def))
		{
			milkProduct = defaultMilkProducts[def];
		}
		else
		{
			if (def.race.Humanlike)
			{
				milkProduct.milkAmount = Mathf.FloorToInt(3f * def.race.baseBodySize / ThingDefOf.Human.race.baseBodySize);
				milkProduct.milkIntervalDays = 0.25f;
				milkProduct.isMilkable = true;
				milkProduct.milkTypeDefName = EMDefOf.EM_HumanMilk.defName;
			}
			else
			{
				milkProduct.isMilkable = false;
				milkProduct.milkAmount = Mathf.FloorToInt(14f * def.race.baseBodySize / ThingDefOf.Cow.race.baseBodySize);
				milkProduct.milkIntervalDays = 1f;
			}
		}
		return milkProduct;
	}
	private void AddOrSetShowProducerComp(ThingDef milkDef)
	{
		CompProperties_ShowProducer compProperties = milkDef.GetCompProperties<CompProperties_ShowProducer>();
		if (compProperties == null)
		{
			compProperties = new CompProperties_ShowProducer();
			milkDef.comps.Add(compProperties);
		}
	}
	/// <summary>建议 22：从 EqualMilkingDefaultsDef 加载关键默认值到当前设置（可被其他 mod patch 的 Def）。</summary>
	public static void ApplyDefaultsFromDef()
	{
		var def = EMDefOf.EM_Defaults;
		if (def == null) return;
		baselineMilkDurationDays = def.baselineMilkDurationDays;
		birthInducedMilkDurationDays = def.birthInducedMilkDurationDays;
		allowMastitis = def.allowMastitis;
		mastitisBaseMtbDays = def.mastitisBaseMtbDays;
		overFullnessRiskMultiplier = def.overFullnessRiskMultiplier;
		hygieneRiskMultiplier = def.hygieneRiskMultiplier;
		allowToleranceAffectMilk = def.allowToleranceAffectMilk;
		toleranceFlowImpactExponent = def.toleranceFlowImpactExponent;
		overflowFilthDefName = def.overflowFilthDefName ?? "Filth_Vomit";
		aiPreferHighFullnessTargets = def.aiPreferHighFullnessTargets;
		defaultFlowMultiplierForHumanlike = def.defaultFlowMultiplierForHumanlike;
	}
	#region Internal setting getters
	internal static bool IsMilkable(string name)
	{
		if (raceCannotLactate != null && raceCannotLactate.Contains(name))
			return false;
		if (raceCanAlwaysLactate != null && raceCanAlwaysLactate.Contains(name))
			return true;
		if (namesToProducts.ContainsKey(name))
		{
			return namesToProducts[name].isMilkable;
		}
		return false;
	}
	internal static bool IsMilkable(Pawn pawn)
	{
		return IsMilkable(pawn.def.defName);
	}
	internal static bool HasPawnTag(Thing thing)
	{
		if (thing == null) { return false; }
		if (productsToTags.ContainsKey(thing.def.defName))
		{
			MilkTag tag = productsToTags[thing.def.defName];
			if (tag.TagPawn && thing is ThingWithComps)
			{
				return true;
			}
		}
		return false;
	}
	internal static bool HasRaceTag(Thing thing)
	{
		if (thing == null) { return false; }
		if (productsToTags.ContainsKey(thing.def.defName))
		{
			MilkTag tag = productsToTags[thing.def.defName];
			if (tag.TagRace && thing is ThingWithComps)
			{
				return true;
			}
		}
		return false;
	}
	internal static bool MilkTypeCanBreastfeed(Pawn mom)
	{
		ThingDef milkDef = mom.MilkDef();
		if (milkDef?.ingestible == null) { return false; }
		if (milkDef.IsNutritionGivingIngestible) { return true; }
		if (milkDef.ingestible.drugCategory == DrugCategory.None && milkDef.ingestible.outcomeDoers.NullOrEmpty()) { return false; }
		return true;
	}
	internal static bool CanBreastfeedEver(Pawn mom, Pawn baby)
	{
		if (mom == baby) { return false; }
		if (mom.RaceProps.Humanlike && humanlikeBreastfeed.AllowBreastfeeding)
		{
			if (baby.RaceProps.Humanlike && humanlikeBreastfeed.BreastfeedHumanlike)
			{
				return true;
			}
			if (baby.RaceProps.Animal && humanlikeBreastfeed.BreastfeedAnimal)
			{
				return true;
			}
			if (baby.RaceProps.IsMechanoid)
			{
				if (humanlikeBreastfeed.BreastfeedMechanoid) { return true; }
				if (MechanitorUtility.IsMechanitor(mom) && MechanitorUtility.GetOverseer(baby) == mom && humanlikeBreastfeed.OverseerBreastfeed) { return true; }
			}
			return false;
		}
		if (mom.RaceProps.Animal && animalBreastfeed.AllowBreastfeeding)
		{
			if (baby.RaceProps.Humanlike && animalBreastfeed.BreastfeedHumanlike)
			{
				return true;
			}
			if (baby.RaceProps.Animal && animalBreastfeed.BreastfeedAnimal)
			{
				return true;
			}
			if (baby.RaceProps.IsMechanoid && animalBreastfeed.BreastfeedMechanoid)
			{
				return true;
			}
			return false;
		}
		if (mom.RaceProps.IsMechanoid && mechanoidBreastfeed.AllowBreastfeeding)
		{
			if (baby.RaceProps.Humanlike && mechanoidBreastfeed.BreastfeedHumanlike)
			{
				return true;
			}
			if (baby.RaceProps.Animal && mechanoidBreastfeed.BreastfeedAnimal)
			{
				return true;
			}
			if (baby.RaceProps.IsMechanoid && mechanoidBreastfeed.BreastfeedMechanoid)
			{
				return true;
			}
			return false;
		}
		return false;
	}
	internal static List<Pawn> GetAutoBreastfeedablePawnsList(Pawn mom)
	{
		if (mom.MilkTypeCanBreastfeed() == false) { return new List<Pawn>(); }
		return mom.MapHeld.mapPawns.AllPawns.Where(baby => mom.AllowedToAutoBreastFeed(baby)).ToList();
	}
	internal static ThingDef GetMilkProductDef(Pawn pawn)
	{
		if (pawn.genes?.GenesListForReading.Where(x => x.Active && x.def.defName.StartsWith(Constants.MILK_TYPE_PREFIX)).FirstOrDefault()?.def is GeneDef geneDef)
		{
			ThingDef thingDef = DefDatabase<ThingDef>.GetNamed(geneDef.defName.Replace(Constants.MILK_TYPE_PREFIX, ""));
			if (thingDef != null)
			{
				return thingDef;
			}
		}
		return namesToProducts.GetWithFallback(pawn.def.defName, new RaceMilkType()).MilkTypeDef;
	}
	internal static float GetMilkAmount(Pawn pawn)
	{
		return namesToProducts.GetWithFallback(pawn.def.defName, new RaceMilkType()).milkAmount * GetMilkAmountFactorWithTolerance(pawn);
	}
	internal static float GetMilkIntervalDays(Pawn pawn)
	{
		return namesToProducts.GetWithFallback(pawn.def.defName, new RaceMilkType()).milkIntervalDays;
	}
	internal static float GetMilkGrowthMultiplier(Pawn pawn)
	{
		return GetLactatingEfficiencyFactorWithTolerance(pawn) * PawnUtility.BodyResourceGrowthSpeed(pawn);
	}
	/// <summary>当前催乳素耐受严重度 t ∈ [0,1]；完全由游戏内 Hediff/Comp 决定。</summary>
	/// <summary>3.3 动物差异化：种族对催乳药物进水倍率，未配置则 1。</summary>
	internal static float GetRaceDrugDeltaSMultiplier(Pawn pawn)
	{
		if (pawn?.def?.defName == null || raceDrugDeltaSMultiplierDefNames == null || raceDrugDeltaSMultiplierValues == null) return 1f;
		int i = raceDrugDeltaSMultiplierDefNames.IndexOf(pawn.def.defName);
		if (i < 0 || i >= raceDrugDeltaSMultiplierValues.Count) return 1f;
		return Mathf.Clamp(raceDrugDeltaSMultiplierValues[i], 0.1f, 3f);
	}

	internal static float GetProlactinTolerance(Pawn pawn)
		=> pawn?.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Tolerance)?.Severity ?? 0f;

	/// <summary>统一耐受系数：E_tol(t) = max(1 − t, 0.05)。</summary>
	internal static float GetProlactinToleranceFactor(Pawn pawn)
	{
		float t = GetProlactinTolerance(pawn);
		return GetProlactinToleranceFactor(t);
	}

	/// <summary>统一耐受系数（按严重度 t）：E_tol(t) = [max(1 − t, 0.05)]^exponent；allowToleranceAffectMilk 关闭时恒为 1。</summary>
	internal static float GetProlactinToleranceFactor(float toleranceSeverity)
	{
		if (!allowToleranceAffectMilk) return 1f;
		float e = Mathf.Max(1f - toleranceSeverity, PoolModelConstants.EffectiveDrugFactorMin);
		return Mathf.Pow(e, Mathf.Clamp(toleranceFlowImpactExponent, 0.1f, 3f));
	}

	internal static float GetMilkAmountFactorWithTolerance(Pawn pawn)
	{
		float v = pawn.GetStatValue(EMDefOf.EM_Milk_Amount_Factor);
		if (v <= 1f) return v;
		float e = GetProlactinToleranceFactor(pawn);
		return 1f + (v - 1f) * e;
	}
	internal static float GetLactatingEfficiencyFactorWithTolerance(Pawn pawn)
	{
		float v = pawn.GetStatValue(EMDefOf.EM_Lactating_Efficiency_Factor);
		if (v <= 0f) return v;
		if (v <= 1f) return v;
		float e = GetProlactinToleranceFactor(pawn);
		return 1f + (v - 1f) * e;
	}
	#endregion
}

/// <summary>建议 13：乳腺炎/风险与耐受相关设置分组，便于序列化与 UI；ExposeData 仍写旧 key 以兼容存档。</summary>
public class MilkRiskSettings : IExposable
{
	public bool allowMastitis = true;
	public float mastitisBaseMtbDays = 1.5f;
	public float overFullnessRiskMultiplier = 1.5f;
	public float hygieneRiskMultiplier = 1f;
	public bool allowToleranceAffectMilk = true;
	public float toleranceFlowImpactExponent = 1f;
	/// <summary>建议 8：人形/动物乳腺炎 MTB 乘数，便于区分平衡。</summary>
	public float mastitisMtbDaysMultiplierHumanlike = 1f;
	public float mastitisMtbDaysMultiplierAnimal = 1f;

	public void ExposeData()
	{
		Scribe_Values.Look(ref allowMastitis, "EM.AllowMastitis", true);
		Scribe_Values.Look(ref mastitisBaseMtbDays, "EM.MastitisBaseMtbDays", 1.5f);
		Scribe_Values.Look(ref overFullnessRiskMultiplier, "EM.OverFullnessRiskMultiplier", 1.5f);
		Scribe_Values.Look(ref hygieneRiskMultiplier, "EM.HygieneRiskMultiplier", 1f);
		Scribe_Values.Look(ref allowToleranceAffectMilk, "EM.AllowToleranceAffectMilk", true);
		Scribe_Values.Look(ref toleranceFlowImpactExponent, "EM.ToleranceFlowImpactExponent", 1f);
		Scribe_Values.Look(ref mastitisMtbDaysMultiplierHumanlike, "EM.MastitisMtbDaysMultiplierHumanlike", 1f);
		Scribe_Values.Look(ref mastitisMtbDaysMultiplierAnimal, "EM.MastitisMtbDaysMultiplierAnimal", 1f);
	}
}
