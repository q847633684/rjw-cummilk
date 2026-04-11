using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using rjw;
using HarmonyLib;
using MilkCum.Fluids.Cum.Common;
using RJW_Menstruation;
using RJW_Menstruation_Fluids;
using RwMenstruationCum = RJW_Menstruation.Cum;
using MilkCum.Fluids.Cum;
using MilkCum.Fluids.Cum.Gathering;

namespace MilkCum.Fluids.Cum.Leaking;

class JobDriver_DeflateDirty : JobDriver_DeflateClean
{
	float amountDeflated;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.GetTarget(TargetIndex.A).Cell, job, 1, -1, null, errorOnFailed);
	}

	public override void DoDeflate()
	{
		base.DoDeflate();
		IEnumerable<FluidsCumWithHediff> sources = CreateFluidList.GetSources(cumflationHediff);
		if (sources.TryRandomElementByWeight(source => source.fluidVolume, out FluidsCumWithHediff chosenFluid))
			SpawnFilth(chosenFluid.sexFluid, chosenFluid);
		else
			SpawnFilth(DefOfs.Cum);
	}

	void SpawnFilth(SexFluidDef sexfluid, FluidsCumWithHediff fluid = null)
	{
		if (sexfluid == null)
			amountDeflated += 0.01f;
		else
			amountDeflated += FluidUtility.GetCumflationFluidCapacity(fluid) * 0.1f;

		FluidGatheringDef fgDef = GatheringUtility.LookupFluidGatheringDef(sexfluid);
		if (fluid?.container != null)
		{
			Hediff containinghediff = fluid.container;
			if (containinghediff.TryGetComp<HediffComp_FluidsCumComp>() != null)
			{
				HediffComp_FluidsCumComp fluidInCumhold = containinghediff.TryGetComp<HediffComp_FluidsCumComp>();
				FluidsCum fluidReference = fluidInCumhold.cumListAnal.FirstOrDefault(f =>
					f.sourcePawn == fluid.sourcePawn && f.fluidVolume == fluid.fluidVolume);
				if (fluidReference == null)
					Log.Error("Couldn't find fluid in anal list");
				else
				{
					fluidReference.fluidVolume -= amountDeflated;
					if (fluidReference.fluidVolume <= 0)
					{
						Log.Warning($"[MilkCum] Deflated too much: {amountDeflated} now at {fluidReference.fluidVolume}");
						fluidReference.fluidVolume = 0;
					}
					fluidInCumhold.SetAnalCumflatedStage();
				}
			}
			else if (containinghediff.def is HediffDef_SexPart sexPart && sexPart.genitalFamily == GenitalFamily.Vagina)
			{
				HediffComp_Menstruation vaginalCumhold = containinghediff.TryGetComp<HediffComp_Menstruation>();
				var field = AccessTools.Field(typeof(HediffComp_Menstruation), "cums");
				var cumList = (List<RwMenstruationCum>)field.GetValue(vaginalCumhold);
				RwMenstruationCum fluidReference = cumList.FirstOrDefault(f => f.pawn == fluid.sourcePawn && f.Volume == fluid.fluidVolume);
				if (fluidReference != null)
				{
					MenstruationCumExtensions.ReduceVolume(fluidReference, amountDeflated);
					if (fluidReference.Volume == 0f)
						Log.Warning($"[MilkCum] deflated too much: {amountDeflated} now at {fluidReference.Volume}");
					CumflationHelper.SetVaginalCumflatedStage(vaginalCumhold);
				}
			}
			else
				Log.Error("[MilkCum] Did not find valid heddiff to remove cum from");
		}

		while (amountDeflated >= fgDef.fluidRequiredForOneUnit / fgDef.filthNecessaryForOneUnit)
		{
			FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, sexfluid.filth);
			FilthProducerRegistry.Record(pawn.Map, pawn.Position, sexfluid.filth, pawn);
			amountDeflated -= fgDef.fluidRequiredForOneUnit / fgDef.filthNecessaryForOneUnit;
		}
	}
}
