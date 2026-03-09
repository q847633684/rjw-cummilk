using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace MilkCum.Fluids.Lactation.Helpers;

public static class MenuHelper
{
    internal static bool ShouldShowInjectMenu(this Thing thing)
    {
        Pawn pawn = thing as Pawn ?? (thing as Building_HoldingPlatform)?.HeldPawn;
        if (pawn == null) { return false; }
        if (!pawn.IsMilkable()) { return false; }
        if (MilkCumSettings.showMechOptions && pawn.IsColonyMech && MechanitorUtility.IsMechanitor(pawn)) { return true; }
        if (MilkCumSettings.showColonistOptions && pawn.IsFreeNonSlaveColonist) { return true; }
        if (MilkCumSettings.showSlaveOptions && pawn.IsSlaveOfColony) { return true; }
        if (MilkCumSettings.showPrisonerOptions && pawn.IsPrisonerOfColony) { return true; }
        if (MilkCumSettings.showAnimalOptions && pawn.IsNormalAnimal() && pawn.Faction == Faction.OfPlayer) { return true; }
        if (MilkCumSettings.showMiscOptions && (pawn.IsOnHoldingPlatform || pawn.Faction == Faction.OfPlayer)) { return true; }
        return false;
    }
    internal static IEnumerable<FloatMenuOption> InjectMenuOptions(this Thing thing, Pawn actor)
    {
        Map map = thing.MapHeld;
        ListerThings listerThings = map.listerThings;
        Pawn pawn = thing as Pawn ?? (thing as Building_HoldingPlatform)?.HeldPawn;
        if (pawn == null) { yield break; } // This should never happen, but just in case
        if (listerThings.ThingsOfDef(MilkCumDefOf.EM_Prolactin).Any(prolactin => prolactin.IsInAnyStorage()) && !(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating)?.Severity >= 1f))
        {
            yield return new FloatMenuOption(Lang.GiveTo(MilkCumDefOf.EM_Prolactin.label, thing.LabelShort), delegate
            {
                Job job = new(MilkCumDefOf.EM_InjectLactatingDrug, listerThings.ThingsOfDef(MilkCumDefOf.EM_Prolactin).FirstOrDefault(), pawn)
                {
                    count = 1
                };
                actor.jobs.TryTakeOrderedJob(job);
            }, MilkCumDefOf.EM_Prolactin);
        }
        if (listerThings.ThingsOfDef(MilkCumDefOf.EM_Lucilactin).Any(lucilactin => lucilactin.IsInAnyStorage()))
        {
            yield return new FloatMenuOption(Lang.GiveTo(MilkCumDefOf.EM_Lucilactin.label, thing.LabelShort), delegate
            {
                Job job = new(MilkCumDefOf.EM_InjectLactatingDrug, listerThings.ThingsOfDef(MilkCumDefOf.EM_Lucilactin).FirstOrDefault(), pawn)
                {
                    count = 1
                };
                actor.jobs.TryTakeOrderedJob(job);
            }, MilkCumDefOf.EM_Lucilactin);
        }
    }
    internal static IEnumerable<FloatMenuOption> BreastfeedMenuOptions(this Thing thing, Pawn actor)
    {
        Pawn pawn = thing as Pawn ?? (thing as Building_HoldingPlatform)?.HeldPawn;
        if (pawn == null) { yield break; }
        // actor 已禁用育儿/哺乳工作类型时，不显示喂奶/吸奶选项
        if (actor.workSettings != null
            && (actor.WorkTypeIsDisabled(WorkTypeDefOf.Childcare) || actor.IsWorkTypeDisabledByAge(WorkTypeDefOf.Childcare, out _)))
        { yield break; }
        // 产奶者（actor）去喂 吸奶者（pawn）：须在产奶者的「谁可以用我的奶」中允许 pawn
        if (actor.CanBreastfeedNow(pawn, out _) && MilkPermissionExtensions.IsAllowedSuckler(actor, pawn))
        {
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(Lang.Join(Lang.Breastfeed, pawn.LabelShort), delegate
                {
                    actor.jobs.TryTakeOrderedJob(new Job(MilkCumDefOf.EM_ForcedBreastfeed, pawn) { count = 1 });
                }, actor.MilkDef()), actor, pawn);
        }
        // 吸奶者（actor）去吸 产奶者（pawn）的奶：须在产奶者（pawn）的「谁可以用我的奶」中允许 actor
        if (pawn.CanBreastfeedNow(actor, out _) && MilkPermissionExtensions.IsAllowedSuckler(pawn, actor))
        {
            string suckleLabel = "EM.MenuSuckleFrom".Translate(pawn.LabelShort);
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(suckleLabel, delegate
            {
                actor.jobs.TryTakeOrderedJob(new Job(MilkCumDefOf.EM_ActiveSuckle, pawn) { count = 1 });
            }, actor.MilkDef()), actor, pawn);
        }
    }
}