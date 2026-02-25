using System.Collections.Generic;
using System.Linq;
using EqualMilking.Helpers;
using EqualMilking.UI;
using EqualMilking.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace EqualMilking;
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
	public static bool showColonistOptions = false;
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
		Scribe_Values.Look(ref showColonistOptions, "EM.ShowColonistOptions", false);
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
			new(Lang.Default, () => tabIndex = 5, tabIndex == 5)
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
				milkProduct.milkTypeDefName = "EM_HumanMilk";
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
	#region Internal setting getters
	internal static bool IsMilkable(string name)
	{
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
	/// <summary>耐受参与加成：有效倍率 = 1 + (原倍率 − 1) × (1 − 耐受)。</summary>
	internal static float GetProlactinTolerance(Pawn pawn) => pawn?.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Tolerance)?.Severity ?? 0f;
	internal static float GetMilkAmountFactorWithTolerance(Pawn pawn)
	{
		float v = pawn.GetStatValue(EMDefOf.EM_Milk_Amount_Factor);
		float t = GetProlactinTolerance(pawn);
		return 1f + (v - 1f) * (1f - t);
	}
	internal static float GetLactatingEfficiencyFactorWithTolerance(Pawn pawn)
	{
		float v = pawn.GetStatValue(EMDefOf.EM_Lactating_Efficiency_Factor);
		float t = GetProlactinTolerance(pawn);
		if (v <= 0f) return v;
		return 1f + (v - 1f) * (1f - t);
	}
	#endregion
}
