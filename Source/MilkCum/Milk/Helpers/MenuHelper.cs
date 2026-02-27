using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MilkCum.Milk.Helpers;

public static class MenuHelper
{
    internal static bool ShouldShowInjectMenu(this Thing thing)
    {
        Pawn pawn = thing as Pawn ?? (thing as Building_HoldingPlatform)?.HeldPawn;
        if (pawn == null) { return false; }
        if (!pawn.IsMilkable()) { return false; }
        if (EqualMilkingSettings.showMechOptions && pawn.IsColonyMech && MechanitorUtility.IsMechanitor(pawn)) { return true; }
        if (EqualMilkingSettings.showColonistOptions && pawn.IsFreeNonSlaveColonist) { return true; }
        if (EqualMilkingSettings.showSlaveOptions && pawn.IsSlaveOfColony) { return true; }
        if (EqualMilkingSettings.showPrisonerOptions && pawn.IsPrisonerOfColony) { return true; }
        if (EqualMilkingSettings.showAnimalOptions && pawn.IsNormalAnimal() && pawn.Faction == Faction.OfPlayer) { return true; }
        if (EqualMilkingSettings.showMiscOptions && (pawn.IsOnHoldingPlatform || pawn.Faction == Faction.OfPlayer)) { return true; }
        return false;
    }
    internal static IEnumerable<FloatMenuOption> InjectMenuOptions(this Thing thing, Pawn actor)
    {
        Map map = thing.MapHeld;
        ListerThings listerThings = map.listerThings;
        Pawn pawn = thing as Pawn ?? (thing as Building_HoldingPlatform)?.HeldPawn;
        if (pawn == null) { yield break; } // This should never happen, but just in case
        if (listerThings.ThingsOfDef(EMDefOf.EM_Prolactin).Any(prolactin => prolactin.IsInAnyStorage()) && !(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating)?.Severity >= 1f))
        {
            yield return new FloatMenuOption(Lang.GiveTo(EMDefOf.EM_Prolactin.label, thing.LabelShort), delegate
            {
                Job job = new(EMDefOf.EM_InjectLactatingDrug, listerThings.ThingsOfDef(EMDefOf.EM_Prolactin).FirstOrDefault(), pawn)
                {
                    count = 1
                };
                actor.jobs.TryTakeOrderedJob(job);
            }, EMDefOf.EM_Prolactin);
        }
        if (listerThings.ThingsOfDef(EMDefOf.EM_Lucilactin).Any(lucilactin => lucilactin.IsInAnyStorage()))
        {
            yield return new FloatMenuOption(Lang.GiveTo(EMDefOf.EM_Lucilactin.label, thing.LabelShort), delegate
            {
                Job job = new(EMDefOf.EM_InjectLactatingDrug, listerThings.ThingsOfDef(EMDefOf.EM_Lucilactin).FirstOrDefault(), pawn)
                {
                    count = 1
                };
                actor.jobs.TryTakeOrderedJob(job);
            }, EMDefOf.EM_Lucilactin);
        }
    }
    internal static IEnumerable<FloatMenuOption> BreastfeedMenuOptions(this Thing thing, Pawn actor)
    {
        Pawn pawn = thing as Pawn ?? (thing as Building_HoldingPlatform)?.HeldPawn;
        if (pawn == null) { yield break; }
        // 仅在实际可以执行时显示选项，不显示灰色的不可用项
        if (actor.CanBreastfeedNow(pawn, out _))
        {
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(Lang.Join(Lang.Breastfeed, pawn.LabelShort), delegate
                {
                    actor.jobs.TryTakeOrderedJob(new Job(EMDefOf.EM_ForcedBreastfeed, pawn) { count = 1 });
                }, actor.MilkDef()), actor, pawn);
        }
        string suckleText = Lang.Join(ThingDefOf.Beer.ingestible.ingestCommandString.Replace("{0}", "SomeonesRoom".Translate().Replace("{PAWN_labelShort}", pawn.LabelShort).Replace("{1}", Lang.Milk)));
        if (pawn.CanBreastfeedNow(actor, out _))
        {
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(suckleText, delegate
            {
                actor.jobs.TryTakeOrderedJob(new Job(EMDefOf.EM_ActiveSuckle, pawn) { count = 1 });
            }, actor.MilkDef()), actor, pawn);
        }
    }
}