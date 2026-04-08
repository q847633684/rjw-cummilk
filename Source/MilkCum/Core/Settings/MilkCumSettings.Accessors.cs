using System.Collections.Generic;
using System.Linq;
using MilkCum.Fluids.Shared.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>`MilkCumSettings` 的查询/计算型访问器分块。</summary>
internal partial class MilkCumSettings
{
	internal static bool IsMilkable(string name)
	{
		if (raceCannotLactate != null && raceCannotLactate.Contains(name)) return false;
		if (raceCanAlwaysLactate != null && raceCanAlwaysLactate.Contains(name)) return true;
		if (namesToProducts.ContainsKey(name)) return namesToProducts[name].isMilkable;
		return false;
	}

	internal static bool IsMilkable(Pawn pawn) => IsMilkable(pawn.def.defName);

	internal static bool HasPawnTag(Thing thing)
	{
		if (thing == null) return false;
		if (!productsToTags.ContainsKey(thing.def.defName)) return false;
		MilkTag tag = productsToTags[thing.def.defName];
		return tag.TagPawn && thing is ThingWithComps;
	}

	internal static bool HasRaceTag(Thing thing)
	{
		if (thing == null) return false;
		if (!productsToTags.ContainsKey(thing.def.defName)) return false;
		MilkTag tag = productsToTags[thing.def.defName];
		return tag.TagRace && thing is ThingWithComps;
	}

	internal static bool IsProducerRestrictionConsumersEffectiveForMilkProducts()
	{
		if (productsToTags.TryGetValue("EM_HumanMilk", out MilkTag t) && t.TagPawn) return true;
		if (productsToTags.TryGetValue("Milk", out t) && t.TagPawn) return true;
		return false;
	}

	internal static bool IsProducerRestrictionConsumersEffectiveForCumProducts()
	{
		if (productsToTags.TryGetValue("Cumpilation_Cum", out MilkTag t) && t.TagPawn) return true;
		return productsToTags.TryGetValue("Cum_Cum", out t) && t.TagPawn;
	}

	internal static bool MilkTypeCanBreastfeed(Pawn mom)
	{
		ThingDef milkDef = mom.MilkDef();
		if (milkDef?.ingestible == null) return false;
		if (milkDef.IsNutritionGivingIngestible) return true;
		return !(milkDef.ingestible.drugCategory == DrugCategory.None && milkDef.ingestible.outcomeDoers.NullOrEmpty());
	}

	internal static bool CanBreastfeedEver(Pawn mom, Pawn baby)
	{
		if (mom == baby) return false;
		if (mom.RaceProps.Humanlike && humanlikeBreastfeed.AllowBreastfeeding)
		{
			if (baby.RaceProps.Humanlike && humanlikeBreastfeed.BreastfeedHumanlike) return true;
			if (baby.RaceProps.Animal && humanlikeBreastfeed.BreastfeedAnimal) return true;
			if (baby.RaceProps.IsMechanoid)
			{
				if (humanlikeBreastfeed.BreastfeedMechanoid) return true;
				if (MechanitorUtility.IsMechanitor(mom) && MechanitorUtility.GetOverseer(baby) == mom && humanlikeBreastfeed.OverseerBreastfeed) return true;
			}
			return false;
		}

		if (mom.RaceProps.Animal && animalBreastfeed.AllowBreastfeeding)
		{
			if (baby.RaceProps.Humanlike && animalBreastfeed.BreastfeedHumanlike) return true;
			if (baby.RaceProps.Animal && animalBreastfeed.BreastfeedAnimal) return true;
			if (baby.RaceProps.IsMechanoid && animalBreastfeed.BreastfeedMechanoid) return true;
			return false;
		}

		if (mom.RaceProps.IsMechanoid && mechanoidBreastfeed.AllowBreastfeeding)
		{
			if (baby.RaceProps.Humanlike && mechanoidBreastfeed.BreastfeedHumanlike) return true;
			if (baby.RaceProps.Animal && mechanoidBreastfeed.BreastfeedAnimal) return true;
			if (baby.RaceProps.IsMechanoid && mechanoidBreastfeed.BreastfeedMechanoid) return true;
			return false;
		}

		return false;
	}

	internal static List<Pawn> GetAutoBreastfeedablePawnsList(Pawn mom)
	{
		if (!mom.MilkTypeCanBreastfeed()) return new List<Pawn>();
		return mom.MapHeld.mapPawns.AllPawns.Where(baby => mom.AllowedToAutoBreastFeed(baby)).ToList();
	}

	internal static ThingDef GetMilkProductDef(Pawn pawn)
	{
		if (pawn.genes?.GenesListForReading.Where(x => x.Active && x.def.defName.StartsWith(MilkCum.Core.Constants.Constants.MILK_TYPE_PREFIX)).FirstOrDefault()?.def is GeneDef geneDef)
		{
			ThingDef thingDef = DefDatabase<ThingDef>.GetNamed(geneDef.defName.Replace(MilkCum.Core.Constants.Constants.MILK_TYPE_PREFIX, ""));
			if (thingDef != null) return thingDef;
		}
		return namesToProducts.GetWithFallback(pawn.def.defName, new RaceMilkType()).MilkTypeDef;
	}

	internal static float GetMilkAmount(Pawn pawn)
	{
		if (pawn?.def == null) return 0f;
		CompProperties_Milkable compMilkable = pawn.def.GetCompProperties<CompProperties_Milkable>();
		if (compMilkable?.milkDef != null && compMilkable.milkAmount > 0f) return compMilkable.milkAmount;
		if (pawn.def.race?.Humanlike == true) return Mathf.FloorToInt(3f * pawn.def.race.baseBodySize / ThingDefOf.Human.race.baseBodySize);
		return Mathf.FloorToInt(14f * pawn.def.race.baseBodySize / ThingDefOf.Cow.race.baseBodySize);
	}

	internal static float GetProlactinTolerance(Pawn pawn)
		=> pawn?.health?.hediffSet?.GetFirstHediffOfDef(MilkCumDefOf.EM_Prolactin_Tolerance)?.Severity ?? 0f;

	/// <summary>水池进水与泌乳流速不再乘本 mod 自算耐受系数；给药合并与 Hediff severity 以原版/XML 为准。</summary>
	internal static float GetProlactinToleranceFactor(Pawn pawn) => 1f;

	/// <summary>兼容旧调用；恒为 1，耐受不再用作泌乳倍率。</summary>
	internal static float GetProlactinToleranceFactor(float toleranceSeverity) => 1f;
}
