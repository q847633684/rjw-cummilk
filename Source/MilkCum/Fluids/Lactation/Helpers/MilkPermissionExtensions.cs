using System;
using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Shared.Comps;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 挤奶/吸奶/哺乳权限与名单：Allow*、Allowed*、名单（allowedSucklers/allowedConsumers）、床主、首次泌乳记忆、成�?育儿文案�?/// �?ExtensionHelper 拆出，见 记忆�?design/架构原则与重组建议�?/// </summary>
public static class MilkPermissionExtensions
{
    #region 开放接口：谁可以使用我的奶 / 奶制品（供其他 mod 覆盖或扩展权限逻辑）

    /// <summary>
    /// 吸奶/挤奶权限的外部处理器。产主 producer 是否允许 doer 使用自己的奶（直接吸奶或挤奶）。
    /// 返回 true=允许，false=不允许，null=交回内置逻辑（名单 + 默认子女+伴侣）。
    /// 按注册顺序调用，第一个返回非 null 的结果即采用。
    /// </summary>
    public static readonly List<Func<Pawn, Pawn, bool?>> AllowSucklerHandlers = new List<Func<Pawn, Pawn, bool?>>();

    /// <summary>
    /// 奶制品食用权限的外部处理器。产主 producer 是否允许 consumer 食用自己的奶制品（含带 CompShowProducer 的奶/精液制品）。
    /// 返回 true=允许，false=不允许，null=交回内置逻辑（allowedConsumers 名单，空=仅产主本人）。
    /// 按注册顺序调用，第一个返回非 null 的结果即采用。
    /// </summary>
    public static readonly List<Func<Pawn, Pawn, Thing, bool?>> CanConsumeMilkProductHandlers = new List<Func<Pawn, Pawn, Thing, bool?>>();

    #endregion
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
        var babyComp = baby.CompEquallyMilkable();
        if (babyComp != null && !babyComp.AllowedToBeAutoFedBy(pawn)) { return false; }
        return true;
    }

    /// <summary>产主 producer 的“父母”之一是否为 doer（用于默认名单排除父母，只保留子女+伴侣）。</summary>
    public static bool IsParentOf(this Pawn producer, Pawn doer)
    {
        if (producer?.relations?.DirectRelations == null || doer == null) return false;
        return producer.relations.DirectRelations.Any(r => r.def == PawnRelationDefOf.Parent && r.otherPawn == doer);
    }

    /// <summary>仅根据关系与设置生成默认允许集合（不查 comp）。供 GetDefaultSucklers 与 IsDefaultAllowedSuckler 共用。</summary>
    public static IEnumerable<Pawn> YieldDefaultSucklers(Pawn producer)
    {
        if (producer?.relations == null) yield break;
        bool incChild = MilkCumSettings.defaultSucklerIncludeChildren;
        bool incLover = MilkCumSettings.defaultSucklerIncludeLover;
        bool incSpouse = MilkCumSettings.defaultSucklerIncludeSpouse;
        bool exclParent = MilkCumSettings.defaultSucklerExcludeParents;
        if (incChild && producer.relations.Children != null)
        {
            foreach (Pawn p in producer.relations.Children)
                if (p != null && !p.Destroyed)
                    yield return p;
        }
        if ((incLover || incSpouse) && producer.relations.DirectRelations != null)
        {
            foreach (DirectPawnRelation r in producer.relations.DirectRelations)
            {
                if (r.otherPawn == null || r.otherPawn.Destroyed) continue;
                if (r.def == PawnRelationDefOf.Lover && !incLover) continue;
                if (r.def == PawnRelationDefOf.Spouse && !incSpouse) continue;
                if (r.def != PawnRelationDefOf.Lover && r.def != PawnRelationDefOf.Spouse) continue;
                if (exclParent && producer.IsParentOf(r.otherPawn)) continue;
                yield return r.otherPawn;
            }
        }
    }

    /// <summary>默认允许集合的单人判断：doer 是否在 YieldDefaultSucklers 中。不分配 List。</summary>
    public static bool IsDefaultAllowedSuckler(Pawn producer, Pawn doer)
    {
        if (producer == null || doer == null) return false;
        foreach (Pawn p in YieldDefaultSucklers(producer))
            if (p == doer) return true;
        return false;
    }

    /// <summary>获取默认“可使用我的奶”名单（用于预填/UI）；逻辑与 YieldDefaultSucklers 一致，去重后返回 List。</summary>
    public static List<Pawn> GetDefaultSucklers(Pawn producer)
    {
        var seen = new HashSet<Pawn>();
        var list = new List<Pawn>();
        foreach (Pawn p in YieldDefaultSucklers(producer))
        {
            if (seen.Add(p)) list.Add(p);
        }
        return list;
    }

    /// <summary>挤奶/吸奶时是否“自愿”：产主允许 doer 使用奶。名单为空时视为默认「仅子女+伴侣、排除父母」；名单非空时仅名单内的人可吸奶/挤奶。优先走 AllowSucklerHandlers 开放接口。</summary>
    public static bool IsAllowedSuckler(Pawn producer, Pawn doer)
    {
        if (producer != null && doer != null && AllowSucklerHandlers.Count > 0)
        {
            foreach (var handler in AllowSucklerHandlers)
            {
                try
                {
                    bool? result = handler(producer, doer);
                    if (result.HasValue) return result.Value;
                }
                catch (Exception)
                {
                    // 单 handler 异常不阻断其他 handler 与内置逻辑
                }
            }
        }
        var comp = producer?.CompEquallyMilkable();
        if (comp == null) return true;
        if (comp.allowedSucklers == null) return false;
        if (comp.allowedSucklers.Count == 0)
            return IsDefaultAllowedSuckler(producer, doer);
        return comp.allowedSucklers.Contains(doer);
    }

    /// <summary>指定谁可以使用产出的奶/精液制品：无 producer 允许；自己始终允许；否则看产主 allowedConsumers，空=仅产主本人。优先走 CanConsumeMilkProductHandlers 开放接口。</summary>
    public static bool CanConsumeMilkProduct(this Pawn consumer, Thing food)
    {
        if (consumer == null || food == null) return true;
        var comp = food.TryGetComp<CompShowProducer>();
        if (comp?.producer == null) return true;
        if (comp.producer == consumer) return true;
        Pawn producer = comp.producer;
        if (CanConsumeMilkProductHandlers.Count > 0)
        {
            foreach (var handler in CanConsumeMilkProductHandlers)
            {
                try
                {
                    bool? result = handler(producer, consumer, food);
                    if (result.HasValue) return result.Value;
                }
                catch (Exception)
                {
                    // 单 handler 异常不阻断其他 handler 与内置逻辑
                }
            }
        }
        var producerComp = producer.CompEquallyMilkable();
        if (producerComp?.allowedConsumers == null || producerComp.allowedConsumers.Count == 0)
            return false; // 仅产主本人（含囚犯/奴隶：未 explicitly 加入名单则殖民者不可食用）
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
