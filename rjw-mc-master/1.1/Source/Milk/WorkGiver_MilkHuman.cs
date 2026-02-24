using System;
using Verse;

namespace Milk
{
	public class WorkGiver_MilkHuman : WorkGiver_GatherHumanBodyResources
	{
		protected override JobDef JobDef
		{
			get
			{
				return JobDefOfZ.MilkHuman;
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
