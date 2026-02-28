using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Milk.Comps;
using MilkCum.Milk.Helpers;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace MilkCum.Milk.Givers;
[StaticConstructorOnStartup]
public class WorkGiver_EquallyMilk : WorkGiver_Milk
{
    protected override CompHasGatherableBodyResource GetComp(Pawn animal)
    {
        return animal.CompEquallyMilkable();
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Pawn target = t as Pawn ?? t?.TryGetComp<CompEntityHolder>()?.HeldPawn;
        if (target == null
        || target == pawn
        || target.IsForbiddenHeld(pawn)
        || target.InAggroMentalState
        || target.IsFormingCaravan()
        || !target.CanCasuallyInteractNow(false, true) //Allow sleeping pawns to be milked
        || target.CurJob?.GetTarget(TargetIndex.A).Thing is Pawn //Prevent interrupt the other pawn's job on other pawns e.g.milking, tend, etc.
        || !target.IsLactating()
        || !ExtensionHelper.IsAllowedSuckler(target, pawn)
        || target.GetDefaultMilkSetting() == null
        || (target.CurJob?.GetTarget(TargetIndex.A).Thing is Building building && building is not Building_Bed)) //TODO properly detect continuous jobs
        {
            return false;
        }
        CompEquallyMilkable comp = target.CompEquallyMilkable();
        if (comp?.ActiveAndFull == true)
        {
            // 若目标同时存在 Cumflation，且严重度远高于奶的 fullness，则优先让泄精 Job 抢人
            var cumflation = Cumpilation.Cumflation.CumflationUtility.GetOrCreateCumflationHediff(target);
            if (cumflation != null && cumflation.Severity > 0.75f && comp.Fullness < comp.maxFullness * 0.75f && !forced)
            {
                return false;
            }
            return pawn.CanReserve(target, 1, -1, null, forced);
        }
        return false;
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        foreach (Pawn target in PotentialWorkThingsGlobal(pawn).OfType<Pawn>())
        {
            CompEquallyMilkable comp = target.CompEquallyMilkable();
            if (comp?.ActiveAndFull == true)
            {
                return false;
            }
        }
        return true;
    }
	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		IEnumerable<Thing> colony = pawn.Map.AllColonyPawns();
		if (!EqualMilkingSettings.aiPreferHighFullnessTargets)
		{
			return colony;
		}
		// 优先满度更高的目标，减少溢出
		return colony
			.OrderByDescending(t =>
			{
				Pawn p = t as Pawn ?? t?.TryGetComp<CompEntityHolder>()?.HeldPawn;
				return p?.CompEquallyMilkable()?.Fullness ?? 0f;
			});
	}
}
