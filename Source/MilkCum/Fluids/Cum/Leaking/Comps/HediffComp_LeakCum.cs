using System.Collections.Generic;
using Verse;
using RimWorld;
using MilkCum.Fluids.Cum.Cumflation;
using MilkCum.Fluids.Cum;
using MilkCum.Fluids.Cum.Gathering;

namespace MilkCum.Fluids.Cum.Leaking
{
	public class HediffComp_LeakCum : HediffComp
	{
		private HediffCompProperties_LeakCum Props => (HediffCompProperties_LeakCum)props;
		private float amountLeaked = 0f;

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			if (!(parent.pawn.TryGetComp(out Comp_SealCum comp) && comp.IsSealed()))
			{
				float num = (parent.Severity + LeakingUtility.GetAverageVaginalLooseness(parent.pawn) + 0.05f) * Props.leakRate * Settings.LeakRate * 0.004f;
				if (Rand.Chance(num))
				{
					parent.Severity -= 0.005f;
                    if (parent.pawn.MapHeld == null) return;	//No filth if pawn is out of map.
                    DropCumFilth();
				}
			}
		}

		private void DropCumFilth()
		{
			if (!Settings.EnableFilthGeneration) { return; }
            rjw.SexFluidDef fluid;
			IEnumerable<FluidSource> sources = parent.TryGetComp<HediffComp_SourceStorage>().sources;
			if (sources.TryRandomElementByWeight(source => source.amount, out FluidSource chosenFluid))
			{
				fluid = chosenFluid.fluid;
			}
			else
			{
				fluid = DefOfs.Cum;
			}
			amountLeaked += CumflationUtility.FluidAmountRequiredToCumflatePawn(parent.pawn, fluid) * 0.005f * Props.leakMult * Settings.LeakMult;
			FluidGatheringDef fgDef = GatheringUtility.LookupFluidGatheringDef(fluid);
			while (amountLeaked >= fgDef.fluidRequiredForOneUnit / fgDef.filthNecessaryForOneUnit)
			{
				FilthMaker.TryMakeFilth(parent.pawn.PositionHeld, parent.pawn.MapHeld, fluid.filth);
				FilthProducerRegistry.Record(parent.pawn.MapHeld, parent.pawn.PositionHeld, fluid.filth, parent.pawn);
				amountLeaked -= fgDef.fluidRequiredForOneUnit / fgDef.filthNecessaryForOneUnit;
			}
		}
	}
}
