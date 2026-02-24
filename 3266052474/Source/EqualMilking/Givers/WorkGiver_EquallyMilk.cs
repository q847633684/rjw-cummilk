using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using RimWorld.Planet;
using System.Linq;
using EqualMilking.Helpers;
namespace EqualMilking;
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
        || !target.AllowMilking()
        || target.GetDefaultMilkSetting() == null
        || (target.CurJob?.GetTarget(TargetIndex.A).Thing is Building building && building is not Building_Bed)) //TODO properly detect continuous jobs
        {
            return false;
        }
        CompEquallyMilkable comp = target.CompEquallyMilkable();
        if (comp?.ActiveAndFull == true)
        {
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
        return pawn.Map.AllColonyPawns();
    }
}
