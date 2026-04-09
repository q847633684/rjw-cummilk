using MilkCum.Core.Settings;
using MilkCum.Fluids.Cum.Gathering;
using MilkCum.Fluids.Shared.Comps;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.MilkingApparel;

/// <summary>阴茎穿戴式采集：解析产物 ThingDef（consumable / FluidGatheringDef），并带产主 Comp 落地。</summary>
public static class MilkingApparelProductUtility
{
    public static bool TryResolvePenisMilkingProduct(ISexPartHediff part, out ThingDef thingDef, out float fluidPerUnit)
    {
        thingDef = null;
        fluidPerUnit = 1f;
        if (part?.Def is not HediffDef_SexPart sdp) return false;
        if (sdp.fluid?.consumable != null)
        {
            thingDef = sdp.fluid.consumable;
            fluidPerUnit = 1f;
            return true;
        }

        SexFluidDef sf = part.GetPartComp()?.Fluid;
        if (sf == null) return false;
        FluidGatheringDef fg = GatheringUtility.LookupFluidGatheringDef(sf);
        if (fg?.thingDef == null) return false;
        thingDef = fg.thingDef;
        fluidPerUnit = Mathf.Max(0.01f, fg.fluidRequiredForOneUnit);
        return true;
    }

    public static void PlaceProductNear(Pawn pawn, ThingDef thingDef, int stackCount)
    {
        if (pawn == null || !pawn.Spawned || thingDef == null || stackCount <= 0) return;
        Thing thing = ThingMaker.MakeThing(thingDef);
        thing.stackCount = stackCount;
        if (thing.TryGetComp<CompShowProducer>() is CompShowProducer show && pawn.RaceProps.Humanlike)
        {
            if (MilkCumSettings.HasRaceTag(thing))
                show.producerKind = pawn.kindDef;
            if (MilkCumSettings.HasPawnTag(thing))
                show.producer = pawn;
        }

        GenPlace.TryPlaceThing(thing, pawn.Position, pawn.MapHeld, ThingPlaceMode.Near);
    }
}
