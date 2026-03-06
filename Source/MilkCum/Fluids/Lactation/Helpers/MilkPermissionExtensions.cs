using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 挤奶/吸奶/哺乳权限与名单：Allow*、Allowed*、名单（allowedSucklers/allowedConsumers）、床主、首次泌乳记忆、成�?育儿文案�?/// �?ExtensionHelper 拆出，见 记忆�?design/架构原则与重组建议�?/// </summary>
public static class MilkPermissionExtensions
{
    public static bool AllowMilking(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.allowMilking ?? false;
    public static bool SetAllowMilking(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.allowMilking = allow;
        return true;
    }
    public static bool AllowToBeFed(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.canBeFed ?? false;
    public static bool SetAllowToBeFed(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.canBeFed = allow;
        return true;
    }
    public static bool AllowMilkingSelf(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.allowMilkingSelf ?? false;
    public static bool SetAllowMilkingSelf(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.allowMilkingSelf = allow;
        return true;
    }
    public static bool AllowBreastFeeding(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.allowBreastFeeding ?? false;
    public static bool SetAllowBreastFeeding(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.allowBreastFeeding = allow;
        return true;
    }
    public static bool AllowBreastFeedingAdult(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.allowBreastFeedingAdult ?? false;
    public static bool SetAllowBreastFeedingAdult(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.allowBreastFeedingAdult = allow;
        return true;
    }
    public static bool AllowBreastFeedByAge(this Pawn pawn, Pawn baby) => pawn != baby && (baby.IsAdult() ? pawn.AllowBreastFeedingAdult() : pawn.AllowBreastFeeding());

    /// <summary>指定“谁可以使用我的奶”：产奶者名单，默认预填子女+伴侣；吸奶与挤奶均按此名单</summary>
    public static bool AllowedToBreastFeed(this Pawn pawn, Pawn baby)
    {
        if (baby?.MapHeld == null || pawn?.MapHeld == null || baby.MapHeld != pawn.MapHeld) { return false; }
        try
        {
            if (baby.IsForbiddenHeld(pawn)) { return false; }
        }
        catch
        {
            return false;
        }
        if (pawn == baby) { return false; }
        if (!pawn.CanBreastfeedEver(baby)) { return false; }
        if (!IsAllowedSuckler(pawn, baby)) { return false; }
        if (!baby.CompEquallyMilkable().AllowedToBeAutoFedBy(pawn)) { return false; }
        return true;
    }

    /// <summary>获取默认“可使用我的奶”名单：子女 + 伴侣（用于名单为空时预填，不再用“空=默认”判断）</summary>
    public static List<Pawn> GetDefaultSucklers(Pawn producer)
    {
        var list = new List<Pawn>();
        if (producer?.relations == null) return list;
        if (producer.relations.Children != null)
        {
            foreach (Pawn p in producer.relations.Children)
                if (p != null && !p.Destroyed && !list.Contains(p))
                    list.Add(p);
        }
        foreach (DirectPawnRelation rel in producer.relations.DirectRelations)
        {
            if (rel.def != PawnRelationDefOf.Lover) continue;
            if (rel.otherPawn != null && !rel.otherPawn.Destroyed && !list.Contains(rel.otherPawn))
                list.Add(rel.otherPawn);
        }
        return list;
    }

    /// <summary>挤奶/吸奶时是否“自愿”：产主允许 doer 使用奶（名单内即可；名单默认预填子女+伴侣）</summary>
    public static bool IsAllowedSuckler(Pawn producer, Pawn doer)
    {
        var comp = producer?.CompEquallyMilkable();
        if (comp == null) return true;
        if (comp.allowedSucklers == null) return false;
        return comp.allowedSucklers.Count > 0 && comp.allowedSucklers.Contains(doer);
    }

    /// <summary>指定谁可以使用产出的�?精液制品：无 producer 允许；自己始终允许；否则看产�?allowedConsumers，空=仅产主本人（囚犯/奴隶亦同，不默认允许殖民者，�?7.4）</summary>
    public static bool CanConsumeMilkProduct(this Pawn consumer, Thing food)
    {
        if (consumer == null || food == null) return true;
        var comp = food.TryGetComp<CompShowProducer>();
        if (comp?.producer == null) return true;
        if (comp.producer == consumer) return true;
        var producerComp = comp.producer.CompEquallyMilkable();
        if (producerComp?.allowedConsumers == null || producerComp.allowedConsumers.Count == 0)
            return false; // 仅产主本人（含囚�?奴隶：未 explicitly 加入名单则殖民者不可食用）
        return producerComp.allowedConsumers.Contains(consumer);
    }

    /// <summary>床的分配对象（床主）。兼容无 AssigningPawn �?RimWorld 版本，通过 CompAssignableToPawn 获取</summary>
    public static Pawn GetBedOwner(this Building_Bed bed)
    {
        if (bed == null) return null;
        var c = bed.GetComp<CompAssignableToPawn>();
        return c?.AssignedPawns?.FirstOrDefault();
    }

    public static bool AllowedToAutoBreastFeed(this Pawn pawn, Pawn baby)
    {
        if (!pawn.AllowedToBreastFeed(baby)) { return false; }
        if (!baby.CompEquallyMilkable().AllowedToBeAutoFedBy(pawn)) { return false; }
        return true;
    }

    /// <summary>分娩后首次泌乳成就类记忆；仅当尚未拥有该记忆时发放，供原�?RJW 分娩入口统一调用</summary>
    public static void TryGiveFirstLactationBirthMemory(Pawn mother)
    {
        if (mother == null || MilkCumDefOf.EM_FirstLactationBirth == null || mother.needs?.mood?.thoughts?.memories == null) return;
        if (mother.needs.mood.thoughts.memories.Memories.Any(m => m.def == MilkCumDefOf.EM_FirstLactationBirth)) return;
        mother.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_FirstLactationBirth);
    }

    public static bool IsAdult(this Pawn pawn)
    {
        if (pawn.ageTracker == null) { return true; }
        if (pawn.RaceProps.Humanlike)
        {
            return (pawn.ageTracker.CurLifeStage?.developmentalStage ?? DevelopmentalStage.Adult) > DevelopmentalStage.Baby;
        }
        if (pawn.IsNormalAnimal())
        {
            return (pawn.ageTracker.CurLifeStage?.developmentalStage ?? DevelopmentalStage.Adult) > DevelopmentalStage.Baby
                && pawn.ageTracker.AgeBiologicalYearsFloat > MilkCumSettings.animalBreastfeed.BabyAge;
        }
        if (pawn.RaceProps.IsMechanoid)
        {
            return (pawn.ageTracker.CurLifeStage?.developmentalStage ?? DevelopmentalStage.Adult) > DevelopmentalStage.Baby
                && pawn.ageTracker.AgeBiologicalYearsFloat > MilkCumSettings.mechanoidBreastfeed.BabyAge;
        }
        return true;
    }

    public static bool CanBreastfeedButNotChildcare(this Pawn pawn, Pawn baby)
    {
        if (pawn.AllowedToAutoBreastFeed(baby) && pawn.IsLactating() // Can breastfeed
            && (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Childcare) || pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Childcare, out _))) // But not childcare
        {
            return true;
        }
        return false;
    }

    public static string ChildcareText(this Pawn pawn, Pawn baby)
    {
        if (pawn.CanBreastfeedButNotChildcare(baby))
        {
            return Lang.Breastfeed;
        }
        if (pawn.AllowedToAutoBreastFeed(baby) && pawn.IsLactating())
        {
            return AutofeedMode.Childcare.Translate().CapitalizeFirst() + " & " + Lang.Breastfeed;
        }
        return AutofeedMode.Childcare.Translate().CapitalizeFirst();
    }
}
