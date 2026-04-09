using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using HarmonyLib;
using rjw;
using RJW_Menstruation;
using RJW_Menstruation_Fluids;
using RwMenstruationCum = RJW_Menstruation.Cum;
using MilkCum.Fluids.Cum;
using MilkCum.Fluids.Cum.Common;
using MilkCum.Fluids.Cum.Gathering;

namespace MilkCum.Fluids.Cum.Leaking;

/// <summary>与 CumpilationLite 一致：为 ExtractCum 手术补充动物 recipeUsers。</summary>
[StaticConstructorOnStartup]
public static class AnimalExtractCumRecipeInjector
{
	static AnimalExtractCumRecipeInjector()
	{
		const string recipeDefName = "ExtractCum";
		RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
		if (recipe == null)
		{
			ModLog.Warning($"Recipe '{recipeDefName}' not found.");
			return;
		}

		foreach (ThingDef td in DefDatabase<ThingDef>.AllDefs.Where(d => d.race != null && d.race.Animal))
		{
			if (!recipe.recipeUsers.Contains(td))
			{
				recipe.recipeUsers.Add(td);
				ModLog.Debug($"Added {td.defName} to recipe '{recipeDefName}'");
			}
		}
	}
}

public class Recipe_ExtractCum : Recipe_Surgery
{
	public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
	{
		if (thing is Pawn pawn && !(MenstruationFluidsCompat.TryGetActiveCumflationForJobs(pawn)?.Severity > 0f))
			return false;
		return base.AvailableOnNow(thing, part);
	}

	public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
	{
		Hediff cumflationHediff = MenstruationFluidsCompat.TryGetActiveCumflationForJobs(pawn);
		if (cumflationHediff?.Severity <= 0 || billDoer == null)
			return;

		List<FluidsCumWithHediff> sources = CreateFluidList.GetSources(cumflationHediff);
		ModLog.Debug($"Got {sources.Count} sources");
		foreach (FluidsCumWithHediff fluid in sources)
		{
				ModLog.Debug($"got fluid with volume: {fluid.fluidVolume}, source: {fluid.sourcePawn?.Name}");
				float amountTotal = 0f;
				if (fluid.container != null)
				{
					ModLog.Debug("removing from cum lists");
					Hediff containinghediff = fluid.container;
					if (containinghediff.TryGetComp<HediffComp_FluidsCumComp>() != null)
					{
						HediffComp_FluidsCumComp fluidInCumhold = containinghediff.TryGetComp<HediffComp_FluidsCumComp>();
						FluidsCum fluidReference = fluidInCumhold.cumListAnal
							.Where(f => f.sourcePawn == fluid.sourcePawn)
							.OrderByDescending(f => f.fluidVolume)
							.FirstOrDefault();
						if (fluidReference == null)
						{
							Log.Error("Couldn't find fluid in anal list");
							continue;
						}

						ModLog.Debug($"Cumflation stats before change: total: {amountTotal}, fluidvolume: {fluidReference.fluidVolume}, hediffseverity: {cumflationHediff.Severity}, total cum in cumhold: {fluidInCumhold.totalCumVolumeAnal}");
						amountTotal += fluidReference.fluidVolume;
						fluidInCumhold.totalCumVolumeAnal -= fluidReference.fluidVolume;
						fluidReference.fluidVolume = 0;
						cumflationHediff = fluidInCumhold.SetAnalCumflatedStage();
						ModLog.Debug(
							$"New cumflation severity: {cumflationHediff?.Severity}, new total: {amountTotal}, fluidvolume: {fluidReference?.fluidVolume}, total cum in cumhold: {fluidInCumhold?.totalCumVolumeAnal}");
					}
					else if (containinghediff.def is HediffDef_SexPart sexPart && sexPart.genitalFamily == GenitalFamily.Vagina)
					{
						HediffComp_Menstruation vaginalCumhold = containinghediff.TryGetComp<HediffComp_Menstruation>();
						var field = AccessTools.Field(typeof(HediffComp_Menstruation), "cums");
						var cumList = (List<RwMenstruationCum>)field.GetValue(vaginalCumhold);
						RwMenstruationCum fluidReference = cumList
							.Where(f => f.pawn == fluid.sourcePawn)
							.OrderByDescending(f => f.Volume)
							.FirstOrDefault();
						if (fluidReference == null)
						{
							Log.Error("Couldn't find fluid in vaginal list");
							continue;
						}

						amountTotal += fluidReference.Volume;
						MenstruationCumExtensions.SetVolumeZero(fluidReference);
						cumflationHediff = CumflationHelper.SetVaginalCumflatedStage(vaginalCumhold);
					}
					else
						Log.Error("Did not find valid heddiff to remove cum from");
				}

				FluidGatheringDef fgDef = GatheringUtility.LookupFluidGatheringDef(fluid.sexFluid);
				ModLog.Debug($"Spawning amount: {amountTotal} in items: {amountTotal / fgDef.fluidRequiredForOneUnit}, per item {fgDef.fluidRequiredForOneUnit}");
				if (amountTotal / fgDef.fluidRequiredForOneUnit < 1f)
					ModLog.Debug($"Amount is less than one with factors: {amountTotal}/{fgDef.fluidRequiredForOneUnit}={amountTotal / fgDef.fluidRequiredForOneUnit}");
				else
					SpawnCumStack(fluid, billDoer, fluid.sexFluid, amountTotal / fgDef.fluidRequiredForOneUnit);
		}
	}

	static void SpawnCumStack(FluidsCumWithHediff fluidCsum, Pawn billDoer, SexFluidDef fluid, float amountToSpawn)
	{
		FluidGatheringDef fgDef = GatheringUtility.LookupFluidGatheringDef(fluid);
		Thing thing = ThingMaker.MakeThing(fgDef.thingDef);
		thing.stackCount = (int)amountToSpawn;
		GenPlace.TryPlaceThing(thing, billDoer.PositionHeld, billDoer.MapHeld, ThingPlaceMode.Near);
	}
}
