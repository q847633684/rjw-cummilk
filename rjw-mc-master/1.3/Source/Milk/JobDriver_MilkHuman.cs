using System;
using Verse;

namespace Milk
{
	public class JobDriver_MilkHuman : JobDriver_GatherHumanBodyResources
	{
		protected override float WorkTotal
		{
			get
			{
				return 400f;
			}
		}

		protected override HumanCompHasGatherableBodyResource GetComp(Pawn animal)
		{
			if (animal.health.hediffSet.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent")))
			{
				return ThingCompUtility.TryGetComp<CompHyperMilkableHuman>(animal);
			}
			return ThingCompUtility.TryGetComp<CompMilkableHuman>(animal);
		}
	}
}