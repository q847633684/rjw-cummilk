using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace EqualMilking.HarmonyPatches;

/// <summary>When a recipe completes, copy producer/producerKind from milk ingredients to products that have CompShowProducer (e.g. Milk made from EM_HumanMilk).</summary>
[HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.Notify_IterationCompleted))]
public static class Bill_Production_Notify_IterationCompleted_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Bill_Production __instance, Pawn billDoer, List<Thing> ingredients)
    {
        if (billDoer?.Map == null || ingredients == null || __instance?.recipe?.products == null) return;

        CompShowProducer source = null;
        foreach (Thing t in ingredients)
        {
            if (t?.def == null) continue;
            var comp = t.TryGetComp<CompShowProducer>();
            if (comp == null) continue;
            if (EqualMilkingSettings.HasPawnTag(t) && comp.producer != null) { source = comp; break; }
            if (EqualMilkingSettings.HasRaceTag(t) && comp.producerKind != null && source == null) source = comp;
        }
        if (source == null) return;

        var productDefs = __instance.recipe.products;
        if (productDefs == null) return;

        var cells = new List<IntVec3> { billDoer.Position };
        if (__instance.billStack?.billGiver is Building b)
        {
            IntVec3 ic = b.InteractionCell;
            if (!cells.Contains(ic)) cells.Add(ic);
        }

        foreach (IntVec3 cell in cells)
        foreach (Thing thing in billDoer.Map.thingGrid.ThingsAt(cell))
        {
            if (thing?.def == null) continue;
            bool isProduct = false;
            for (int i = 0; i < productDefs.Count; i++)
            {
                if (productDefs[i].thingDef == thing.def) { isProduct = true; break; }
            }
            if (!isProduct) continue;
            var comp = thing.TryGetComp<CompShowProducer>();
            if (comp == null) continue;
            comp.producer = source.producer;
            comp.producerKind = source.producerKind;
        }
    }
}
