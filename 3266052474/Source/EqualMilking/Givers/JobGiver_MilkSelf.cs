using Verse;
using Verse.AI;
using RimWorld;
using EqualMilking.Helpers;
using System.Linq;

namespace EqualMilking;
/// <summary>
/// Allow non-humanlike to breastfeed
/// </summary>
public class JobGiver_MilkSelf : ThinkNode_JobGiver
{
	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.GetDefaultMilkSetting() == null) { return null; }
		if (pawn.CanReserve(pawn, 1, -1, null, false)
			&& pawn.AllowMilkingSelf()
			&& pawn.IsLactating()
			&& pawn.CompEquallyMilkable().ActiveAndFull)
		{
			Building_Milking milkingSpot = pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Milking>()
																				.Where(x => x.CanBeUsedBy(pawn) && pawn.CanReserveAndReach(x, PathEndMode.OnCell, Danger.Deadly))
																				.OrderByDescending(x => x.YieldOffset())
																				.ThenBy(x => x.PositionHeld.DistanceToSquared(pawn.PositionHeld))
																				.FirstOrDefault();
			if (pawn.GetStatValue(StatDefOf.AnimalGatherYield) + (milkingSpot?.YieldOffset() ?? 0f) > 0)
			{
				return JobMaker.MakeJob(JobDefOf.Milk, pawn, milkingSpot);
			}
		}
		return null;
	}
}
