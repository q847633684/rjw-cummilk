using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using MilkCum.Milk.Helpers;
namespace MilkCum.Milk.Givers;
[StaticConstructorOnStartup]
public class WorkGiver_EquallyMilkEntity : WorkGiver_EquallyMilk
{
    /// <summary>
    /// Somehow potential work thing global just doesn't process entities, Must use potential work thing request
    /// </summary>
    public override ThingRequest PotentialWorkThingRequest
    {
        get
        {
            return ThingRequest.ForGroup(ThingRequestGroup.EntityHolder);
        }
    }
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        foreach (Pawn colonyPawn in pawn.Map.AllColonyPawns().Where(p => p.IsEntity))
        {
            if (colonyPawn.CompEquallyMilkable().ActiveAndFull)
            {
                return false;
            }
        }
        return true;
    }
    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        return null;
    }
    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return base.JobOnThing(pawn, t?.TryGetComp<CompEntityHolder>()?.HeldPawn, forced);
    }
}
