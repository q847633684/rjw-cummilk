using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using rjw;
using HarmonyLib;
using RJW_Menstruation;
using RJW_Menstruation_Fluids;
using RwMenstruationCum = RJW_Menstruation.Cum;
using MilkCum.Fluids.Cum;
using MilkCum.Fluids.Cum.Common;
using MilkCum.Fluids.Cum.Gathering;

namespace MilkCum.Fluids.Cum.Leaking;

class JobDriver_DeflateBucket : JobDriver_DeflateClean
{
	float amountDeflated;

	public override void DoDeflate()
	{
		base.DoDeflate();
		IEnumerable<FluidsCumWithHediff> sources = CreateFluidList.GetSources(cumflationHediff);
		if (sources.TryRandomElementByWeight(source => source.fluidVolume, out FluidsCumWithHediff chosenFluid))
			SpawnCum(chosenFluid.sexFluid, chosenFluid);
		else
			SpawnCum(DefOfs.Cum);
	}

	void SpawnCum(SexFluidDef sexfluid, FluidsCumWithHediff fluid = null)
	{
		if (fluid == null)
		{
			ModLog.Debug("Creating placeholder fluid hediff");
			fluid = new FluidsCumWithHediff
			{
				sourcePawn = pawn,
				sexFluid = sexfluid,
				fluidVolume = 1f,
				fluidColor = new Color(1f, 1f, 1f),
				container = null
			};
		}

		float deflateMult = TargetA.Thing?.TryGetComp<Comp_DeflateBucket>()?.deflateMult ?? 1f;
		float newamountDeflated = Math.Max((fluid.fluidVolume - FluidUtility.GetCumflationFluidCapacity(fluid)) / 10f, 1f) * 5f * deflateMult * Settings.DeflateMult;
		amountDeflated += newamountDeflated;
		FluidGatheringDef fgDef = GatheringUtility.LookupFluidGatheringDef(sexfluid);
		ModLog.Debug($"deflating {amountDeflated}, new: {newamountDeflated}, capacity: {FluidUtility.GetCumflationFluidCapacity(fluid)}, mult {deflateMult}, per item {fgDef.fluidRequiredForOneUnit}");

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
					Log.Error("Couldn't find fluid in anal list");
				else
					ModLog.Debug($"Remove successful, now at {fluidReference.fluidVolume}");
				cumflationHediff = fluidInCumhold.SetAnalCumflatedStage();
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
					Log.Error("Couldn't find fluid in vaginal list");
				else
				{
					MenstruationCumExtensions.ReduceVolume(fluidReference, newamountDeflated);
					if (fluidReference.Volume == 0f)
						Log.Warning($"[MilkCum] deflated too much: {newamountDeflated} now at {fluidReference.Volume}");
					else
						ModLog.Debug($"Remove successful, now at {fluidReference.Volume}");
					cumflationHediff = CumflationHelper.SetVaginalCumflatedStage(vaginalCumhold);
				}
			}
			else
				Log.Error("[MilkCum] Did not find valid heddiff to remove cum from");
		}

		while (amountDeflated >= fgDef.fluidRequiredForOneUnit)
		{
			Thing thing = ThingMaker.MakeThing(fgDef.thingDef);
			GenPlace.TryPlaceThing(thing, job.GetTarget(TargetIndex.A).Cell, pawn.Map, ThingPlaceMode.Near);
			pawn.Reserve(thing, job, 1, -1, null);
			amountDeflated -= fgDef.fluidRequiredForOneUnit;
		}
	}
}
