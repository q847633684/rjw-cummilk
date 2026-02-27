using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MilkCum.Core;
public class Building_Milking : Building_Storage
{
    public bool IsPowered => this.TryGetComp<CompPowerTrader>()?.PowerOn ?? false;
    public bool IsPoweredOrManual => this.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;
    public void PlaceMilkThing(Thing milkThing)
    {
        GenPlace.TryPlaceThing(milkThing, this.Position, this.Map, ThingPlaceMode.Near);
    }
    public bool CanBeUsedBy(Pawn pawn)
    {
        return !this.IsForbiddenHeld(pawn) && (this.IsPowered || (pawn.RaceProps.Humanlike && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)));
    }
    public float SpeedOffset()
    {
        if (!IsPoweredOrManual) { return 0f; }
        return this.def.equippedStatOffsets?.GetStatOffsetFromList(StatDefOf.AnimalGatherSpeed) ?? 0f;
    }
    public float YieldOffset()
    {
        if (!IsPoweredOrManual) { return 0f; }
        return this.def.equippedStatOffsets?.GetStatOffsetFromList(StatDefOf.AnimalGatherYield) ?? 0f;
    }
    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            // Remove storage gizmos
            if (gizmo is Command_Action command_Action && (command_Action.defaultLabel == "CommandCopyZoneSettingsLabel".Translate()
                                                        || command_Action.defaultLabel == "CommandPasteZoneSettingsLabel".Translate()
                                                        || command_Action.defaultLabel == "LinkStorageSettings".Translate()))
            { continue; }
            yield return gizmo;
        }
    }
}
