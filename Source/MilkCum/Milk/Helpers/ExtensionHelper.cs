using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Milk.Comps;
using MilkCum.Milk.Data;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

using static MilkCum.Milk.Helpers.Categories;

namespace MilkCum.Milk.Helpers;

public static class ExtensionHelper
{
    private const int LactatingStateCacheTicks = 60;
    private static readonly Dictionary<Pawn, (bool value, int tick)> LactatingStateCache = new();

    public static MilkSettings GetDefaultMilkSetting(this Pawn pawn)
    {
        if (pawn == null) { return null; }
        return GetPawnCategory(pawn) switch
        {
            PawnCategory.Colonist => EqualMilkingSettings.colonistSetting.Copy(),
            PawnCategory.Prisoner => EqualMilkingSettings.prisonerSetting.Copy(),
            PawnCategory.Slave => EqualMilkingSettings.slaveSetting.Copy(),
            PawnCategory.Animal => EqualMilkingSettings.animalSetting.Copy(),
            PawnCategory.Mechanoid => EqualMilkingSettings.mechSetting.Copy(),
            PawnCategory.Entity => EqualMilkingSettings.entitySetting.Copy(),
            _ => null,
        };
    }
    public static float BaseNutritionPerDay(this Pawn p)
    {
        return p.ageTracker.CurLifeStage.hungerRateFactor * p.RaceProps.baseHungerRate * 2.6666667E-05f * 60000f;
    }
    #region Breastfeeding
    public static bool IsMilkable(this Pawn pawn) => EqualMilkingSettings.IsMilkable(pawn);
    public static bool HasNutritiousMilk(this Pawn pawn) => pawn.IsMilkable() && (pawn.MilkDef().ingestible?.CachedNutrition ?? 0) > 0;
    public static bool HasDrugMilk(this Pawn pawn) => pawn.IsMilkable() && ((pawn.MilkDef().ingestible?.drugCategory ?? DrugCategory.None) != DrugCategory.None);
    public static bool HasEdibleMilk(this Pawn pawn) => pawn.HasNutritiousMilk() || pawn.HasDrugMilk();
    public static bool IsLactating(this Pawn pawn) => pawn?.health?.hediffSet?.HasHediff(HediffDefOf.Lactating) ?? false;
    /// <summary>是否有药物诱发的泌乳（催乳素耐受或成瘾），用于显示/添加「药物泌乳负担」等。</summary>
    public static bool HasDrugInducedLactation(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return false;
        return (global::MilkCum.Core.EMDefOf.EM_Prolactin_Tolerance != null && pawn.health.hediffSet.GetFirstHediffOfDef(global::MilkCum.Core.EMDefOf.EM_Prolactin_Tolerance) != null)
            || (global::MilkCum.Core.EMDefOf.EM_Prolactin_Addiction != null && pawn.health.hediffSet.GetFirstHediffOfDef(global::MilkCum.Core.EMDefOf.EM_Prolactin_Addiction) != null);
    }
    /// <summary>统一泌乳判断：本体 Lactating 或 RJW 系 Lactating_Drug / Lactating_Permanent / Heavy_Lactating_Permanent。带 60 tick 缓存。</summary>
    public static bool IsInLactatingState(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return false;
        int now = Find.TickManager.TicksGame;
        if (LactatingStateCache.TryGetValue(pawn, out var cached) && now - cached.tick < LactatingStateCacheTicks)
            return cached.value;
        bool value = pawn.health.hediffSet.HasHediff(HediffDefOf.Lactating)
            || (DefDatabase<HediffDef>.GetNamedSilentFail("Lactating_Drug") is HediffDef d1 && pawn.health.hediffSet.HasHediff(d1))
            || (DefDatabase<HediffDef>.GetNamedSilentFail("Lactating_Permanent") is HediffDef d2 && pawn.health.hediffSet.HasHediff(d2))
            || (DefDatabase<HediffDef>.GetNamedSilentFail("Heavy_Lactating_Permanent") is HediffDef d3 && pawn.health.hediffSet.HasHediff(d3));
        LactatingStateCache[pawn] = (value, now);
        return value;
    }
    /// <summary>清除泌乳缓存（如 hediff 变更时可调，非必须）。</summary>
    public static void ClearLactatingStateCache(Pawn pawn = null)
    {
        if (pawn == null) LactatingStateCache.Clear();
        else LactatingStateCache.Remove(pawn);
    }
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
    /// <summary>指定“谁可以使用我的奶”：产奶者名单，默认预填子女+伴侣；吸奶与挤奶均按此名单。</summary>
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

    /// <summary>获取默认“可使用我的奶”名单：子女 + 伴侣（用于名单为空时预填，不再用“空=默认”判断）。</summary>
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

    /// <summary>挤奶/吸奶时是否“自愿”：产主允许 doer 使用奶（名单内即可；名单默认预填子女+伴侣）。</summary>
    public static bool IsAllowedSuckler(Pawn producer, Pawn doer)
    {
        var comp = producer?.CompEquallyMilkable();
        if (comp == null) return true;
        if (comp.allowedSucklers == null) return false;
        return comp.allowedSucklers.Count > 0 && comp.allowedSucklers.Contains(doer);
    }

    /// <summary>指定谁可以使用产出的奶/精液制品：无 producer 允许；自己始终允许；否则看产主 allowedConsumers，空=仅产主本人（囚犯/奴隶亦同，不默认允许殖民者，见 7.4）。</summary>
    public static bool CanConsumeMilkProduct(this Pawn consumer, Thing food)
    {
        if (consumer == null || food == null) return true;
        var comp = food.TryGetComp<CompShowProducer>();
        if (comp?.producer == null) return true;
        if (comp.producer == consumer) return true;
        var producerComp = comp.producer.CompEquallyMilkable();
        if (producerComp?.allowedConsumers == null || producerComp.allowedConsumers.Count == 0)
            return false; // 仅产主本人（含囚犯/奴隶：未 explicitly 加入名单则殖民者不可食用）
        return producerComp.allowedConsumers.Contains(consumer);
    }

    /// <summary>床的分配对象（床主）。兼容无 AssigningPawn 的 RimWorld 版本，通过 CompAssignableToPawn 获取。</summary>
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
                && pawn.ageTracker.AgeBiologicalYearsFloat > EqualMilkingSettings.animalBreastfeed.BabyAge;
        }
        else if (pawn.RaceProps.IsMechanoid)
        {
            return (pawn.ageTracker.CurLifeStage?.developmentalStage ?? DevelopmentalStage.Adult) > DevelopmentalStage.Baby
                && pawn.ageTracker.AgeBiologicalYearsFloat > EqualMilkingSettings.mechanoidBreastfeed.BabyAge;
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
    #endregion
    #region Milk
    /// <summary>按 defName 取左乳 hediff（Breast_Left）。不依赖索引、不判断人形/动物。</summary>
    public static Hediff GetLeftBreast(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.hediffs == null) return null;
        return pawn.health.hediffSet.hediffs
            .FirstOrDefault(h => h?.def?.defName == "Breast_Left");
    }
    /// <summary>按 defName 取右乳 hediff（Breast_Right）。不依赖索引、不判断人形/动物。</summary>
    public static Hediff GetRightBreast(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.hediffs == null) return null;
        return pawn.health.hediffSet.hediffs
            .FirstOrDefault(h => h?.def?.defName == "Breast_Right");
    }

    /// <summary>兼容旧 RJW：若仅有原版一对“Breasts”（1 个 hediff）且尚无 Breast_Left，则拆分为 Breast_Left + Breast_Right（各一半 Severity），并清除 RJW 缓存。人形才拆分；动物单乳不拆。</summary>
    public static void TrySplitOldBreastsToLeftRight(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return;
        if (pawn.GetLeftBreast() != null) return;
        var list = pawn.GetBreastList();
        if (list == null || list.Count != 1) return;
        var old = list[0];
        if (old?.def == null || (old.def.defName != "Breasts" && old.def.defName != "GenericBreasts")) return;
        if (pawn.RaceProps?.Humanlike != true) return;
        var leftDef = DefDatabase<HediffDef>.GetNamedSilentFail("Breast_Left");
        var rightDef = DefDatabase<HediffDef>.GetNamedSilentFail("Breast_Right");
        if (leftDef == null || rightDef == null) return;
        var part = pawn.GetBreastOrChestPart();
        if (part == null) return;
        float halfSeverity = Mathf.Clamp(old.Severity * 0.5f, 0.01f, 10f);
        var leftHediff = pawn.health.AddHediff(leftDef, part);
        if (leftHediff != null) leftHediff.Severity = halfSeverity;
        var rightHediff = pawn.health.AddHediff(rightDef, part);
        if (rightHediff != null) rightHediff.Severity = halfSeverity;
        pawn.health.RemoveHediff(old);
        try { pawn.GetRJWPawnData().breasts = null; } catch { }
    }

    /// <summary>左池容量：来自左乳 hediff（Breast_Left）Severity；无独立左乳时走兼容逻辑（拆分或按 GetBreastList 回退）。</summary>
    public static float GetLeftBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out float left, out _);
        return left;
    }
    /// <summary>右池容量：来自右乳 hediff（Breast_Right）Severity；无独立右乳时走兼容逻辑。</summary>
    public static float GetRightBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out _, out float right);
        return right;
    }
    /// <summary>左池/右池容量。优先用独立左右乳（GetLeftBreast/GetRightBreast）；若无左乳且仅有一对旧 Breasts 则先 TrySplitOldBreastsToLeftRight 再取。未启用 RJW 乳房尺寸时人形 0.5+0.5。GetBreastList 返回「胸部部位上的性部位 hediff 列表」。左右判定优先用 BodyPartRecord：h.Part.def.defName 以 LeftBreast/RightBreast 开头时归左/右池（BodyDef 例：Torso 下 LeftBreast_1、LeftBreast_2、RightBreast_1、RightBreast_2，每侧 N 个独立）；其次用 HediffDef.defName Breast_Left/Breast_Right；其余 def 人形与动物均按一对对半归左右池。</summary>
    private static void GetBreastCapacityFactors(Pawn pawn, out float leftFactor, out float rightFactor)
    {
        leftFactor = 0f;
        rightFactor = 0f;
        if (pawn == null) return;
        if (!EqualMilkingSettings.rjwBreastSizeEnabled)
        {
            if (pawn.RaceProps?.Humanlike == true) { leftFactor = 0.5f; rightFactor = 0.5f; }
            return;
        }
        float coeff = EqualMilkingSettings.rjwBreastCapacityCoefficient;
        try
        {
            if (pawn.GetLeftBreast() == null && pawn.GetBreastList() is { Count: 1 })
                pawn.TrySplitOldBreastsToLeftRight();
            var left = pawn.GetLeftBreast();
            var right = pawn.GetRightBreast();
            if (left != null && right != null)
            {
                leftFactor = Mathf.Clamp(left.Severity * coeff, 0.01f, 10f);
                rightFactor = Mathf.Clamp(right.Severity * coeff, 0.01f, 10f);
                return;
            }
            var list = pawn.GetBreastList();
            if (list.Count >= 1)
            {
                foreach (var h in list)
                {
                    if (h?.def == null) continue;
                    float cap = Mathf.Clamp(h.Severity * coeff, 0.01f, 10f);
                    string partDefName = h.Part?.def?.defName;
                    string dn = h.def.defName;
                    // BodyPartRecord 区分左右：BodyDef 下可挂 LeftBreast_1/LeftBreast_2、RightBreast_1/RightBreast_2 等，每侧 N 个独立
                    bool isRight = (partDefName != null && partDefName.StartsWith("RightBreast")) || (dn == "Breast_Right");
                    bool isLeft = (partDefName != null && partDefName.StartsWith("LeftBreast")) || (dn == "Breast_Left");
                    if (isRight)
                        rightFactor += cap;
                    else if (isLeft)
                        leftFactor += cap;
                    else
                    {
                        // 无 Part/defName 左右信息时，人形与动物均按一对对半归左右池
                        leftFactor += cap * 0.5f;
                        rightFactor += cap * 0.5f;
                    }
                }
                if (leftFactor >= 0.01f || rightFactor >= 0.01f)
                    return;
            }
            if (pawn.RaceProps?.Humanlike == true) { leftFactor = 0.5f; rightFactor = 0.5f; }
        }
        catch
        {
            if (pawn.RaceProps?.Humanlike == true) { leftFactor = 0.5f; rightFactor = 0.5f; }
        }
    }

    /// <summary>按单乳枚举池条目（左1、右1、左2、右2…），用于独立进水与展示每乳产奶。key 稳定为 Part.def.defName 或 defName_index；无左右信息时拆成 _L/_R 两键。同一对左右共享 PairIndex，用于按对撑大与挤奶顺序（从第一对开始）。</summary>
    public static List<BreastPoolEntry> GetBreastPoolEntries(this Pawn pawn)
    {
        var result = new List<BreastPoolEntry>();
        if (pawn == null) return result;
        if (!EqualMilkingSettings.rjwBreastSizeEnabled)
        {
            if (pawn.RaceProps?.Humanlike == true)
            {
                result.Add(new BreastPoolEntry("Left_Default", 0.5f, 0.5f, true, 0));
                result.Add(new BreastPoolEntry("Right_Default", 0.5f, 0.5f, false, 0));
            }
            return result;
        }
        try
        {
            if (pawn.GetLeftBreast() == null && pawn.GetBreastList() is { Count: 1 })
                pawn.TrySplitOldBreastsToLeftRight();
            var list = pawn.GetBreastList();
            float coeff = EqualMilkingSettings.rjwBreastCapacityCoefficient;
            int currentPair = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (h?.def == null) continue;
                string partDefName = h.Part?.def?.defName;
                string dn = h.def.defName;
                string key = !string.IsNullOrEmpty(partDefName) ? partDefName : dn + "_" + i;
                float cap = Mathf.Clamp(h.Severity * coeff, 0.01f, 10f);
                float mult = (h.def is HediffDef_SexPart d) ? Mathf.Clamp(d.fluidMultiplier, 0.1f, 3f) : 1f;
                bool isRight = (partDefName != null && partDefName.StartsWith("RightBreast")) || (dn == "Breast_Right");
                bool isLeft = (partDefName != null && partDefName.StartsWith("LeftBreast")) || (dn == "Breast_Left");
                if (isRight)
                    result.Add(new BreastPoolEntry(key, cap, mult, false, currentPair));
                else if (isLeft)
                {
                    result.Add(new BreastPoolEntry(key, cap, mult, true, currentPair));
                    currentPair++;
                }
                else
                {
                    result.Add(new BreastPoolEntry(key + "_L", cap * 0.5f, mult * 0.5f, true, currentPair));
                    result.Add(new BreastPoolEntry(key + "_R", cap * 0.5f, mult * 0.5f, false, currentPair));
                    currentPair++;
                }
            }
            if (result.Count == 0 && pawn.RaceProps?.Humanlike == true)
            {
                result.Add(new BreastPoolEntry("Left_Default", 0.5f, 0.5f, true, 0));
                result.Add(new BreastPoolEntry("Right_Default", 0.5f, 0.5f, false, 0));
            }
        }
        catch
        {
            if (pawn.RaceProps?.Humanlike == true)
            {
                result.Clear();
                result.Add(new BreastPoolEntry("Left_Default", 0.5f, 0.5f, true, 0));
                result.Add(new BreastPoolEntry("Right_Default", 0.5f, 0.5f, false, 0));
            }
        }
        return result;
    }
    public static ThingDef MilkDef(this Pawn pawn) => EqualMilkingSettings.GetMilkProductDef(pawn);
    public static float MilkAmount(this Pawn pawn) => EqualMilkingSettings.GetMilkAmount(pawn);
    public static float MilkGrowthMultiplier(this Pawn pawn) => EqualMilkingSettings.GetMilkGrowthMultiplier(pawn);
    public static float MilkMarketValue(this Pawn pawn) => pawn.MilkDef()?.BaseMarketValue ?? 0;
    public static bool MilkTypeCanBreastfeed(this Pawn mom) => EqualMilkingSettings.MilkTypeCanBreastfeed(mom);
    public static bool CanBreastfeedEver(this Pawn mom, Pawn baby) => EqualMilkingSettings.CanBreastfeedEver(mom, baby);
    public static HediffComp_EqualMilkingLactating LactatingHediffComp(this Pawn pawn) => pawn.health.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating)?.TryGetComp<HediffComp_EqualMilkingLactating>();
    public static Hediff LactatingHediff(this Pawn pawn) => pawn.health.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating);
    public static HediffWithComps_EqualMilkingLactating LactatingHediffWithComps(this Pawn pawn) => pawn.LactatingHediff() as HediffWithComps_EqualMilkingLactating;

    /// <summary>规格：乳腺炎/堵塞等健康影响进水流速。返回 1f 减去乳房不适类 hediff 的惩罚（severity×0.5），最小 0.5。</summary>
    public static float GetMilkFlowMultiplierFromConditions(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return 1f;
        var mastitis = global::MilkCum.Core.EMDefOf.EM_Mastitis != null ? pawn.health.hediffSet.GetFirstHediffOfDef(global::MilkCum.Core.EMDefOf.EM_Mastitis) : null;
        if (mastitis == null) return 1f;
        float penalty = mastitis.Severity * 0.5f;
        return Mathf.Clamp(1f - penalty, 0.5f, 1f);
    }

    /// <summary>RJW-Genes 兼容：基因对泌乳流速的倍率。无基因或未安装 rjw-genes 时返回 1f；如 rjw_genes_big_breasts / rjw_genes_extra_breasts 等可略微提高流速。</summary>
    public static float GetMilkFlowMultiplierFromGenes(this Pawn pawn)
    {
        if (pawn?.genes == null || !pawn.RaceProps.Humanlike) return 1f;
        float mult = 1f;
        var bigBreasts = DefDatabase<GeneDef>.GetNamedSilentFail("rjw_genes_big_breasts");
        if (bigBreasts != null && pawn.genes.HasActiveGene(bigBreasts))
            mult *= 1.12f;
        var extraBreasts = DefDatabase<GeneDef>.GetNamedSilentFail("rjw_genes_extra_breasts");
        if (extraBreasts != null && pawn.genes.HasActiveGene(extraBreasts))
            mult *= 1.08f;
        return Mathf.Clamp(mult, 0.5f, 1.5f);
    }

    /// <summary>左乳流速倍率：左侧所有乳 hediff 的 fluidMultiplier 之和（与容量一致：Part.def 以 LeftBreast 开头或 defName Breast_Left），支持多乳；无左侧或非 SexPart 时 0f；未启用 RJW 乳房尺寸时 1f。与 GetMilkFlowMultiplierFromRJW_Right 同源，建议用 GetMilkFlowMultipliersFromRJW 一次取两侧。</summary>
    public static float GetMilkFlowMultiplierFromRJW_Left(this Pawn pawn)
    {
        pawn.GetMilkFlowMultipliersFromRJW(out float left, out _);
        return left;
    }
    /// <summary>右乳流速倍率：右侧所有乳 hediff 的 fluidMultiplier 之和（Part.def 以 RightBreast 开头或 defName Breast_Right），支持多乳；无右侧或非 SexPart 时 0f；未启用时 1f。</summary>
    public static float GetMilkFlowMultiplierFromRJW_Right(this Pawn pawn)
    {
        pawn.GetMilkFlowMultipliersFromRJW(out _, out float right);
        return right;
    }

    /// <summary>左右池流速倍率（一次遍历 GetBreastList，与容量判定一致：LeftBreast*/RightBreast* 或 Breast_Left/Breast_Right）。每侧 N 个乳则 N 个 fluidMultiplier 相加；两侧均为 0 时返回 0.5、0.5（50/50）。</summary>
    public static void GetMilkFlowMultipliersFromRJW(this Pawn pawn, out float leftMultiplier, out float rightMultiplier)
    {
        leftMultiplier = 0f;
        rightMultiplier = 0f;
        if (pawn == null || !EqualMilkingSettings.rjwBreastSizeEnabled)
        {
            leftMultiplier = 1f;
            rightMultiplier = 1f;
            return;
        }
        try
        {
            var list = pawn.GetBreastList();
            foreach (var h in list)
            {
                if (h?.def == null) continue;
                string partDefName = h.Part?.def?.defName;
                string dn = h.def.defName;
                bool isRight = (partDefName != null && partDefName.StartsWith("RightBreast")) || (dn == "Breast_Right");
                bool isLeft = (partDefName != null && partDefName.StartsWith("LeftBreast")) || (dn == "Breast_Left");
                float mult = (h.def is HediffDef_SexPart d) ? Mathf.Clamp(d.fluidMultiplier, 0.1f, 3f) : 1f;
                if (isRight) rightMultiplier += mult;
                else if (isLeft) leftMultiplier += mult;
                else
                {
                    leftMultiplier += mult * 0.5f;
                    rightMultiplier += mult * 0.5f;
                }
            }
            if (leftMultiplier <= 0f && rightMultiplier <= 0f)
            {
                leftMultiplier = 0.5f;
                rightMultiplier = 0.5f;
            }
        }
        catch
        {
            leftMultiplier = 0.5f;
            rightMultiplier = 0.5f;
        }
    }

    /// <summary>健康页悬停：给定乳房 hediff（须在 GetBreastList 中），返回其对应的池 key（与 GetBreastPoolEntries 一致）。</summary>
    public static string GetPoolKeyForBreastHediff(this Pawn pawn, Hediff breastHediff)
    {
        if (pawn == null || breastHediff == null) return null;
        var list = pawn.GetBreastList();
        if (list == null || !list.Contains(breastHediff)) return null;
        int i = list.IndexOf(breastHediff);
        string partDefName = breastHediff.Part?.def?.defName;
        return !string.IsNullOrEmpty(partDefName) ? partDefName : breastHediff.def.defName + "_" + i;
    }

    /// <summary>健康页悬停（多乳）：返回该 hediff 对应的所有池条目（1 条为单侧乳，2 条为拆成 _L/_R 的一对），用于显示「左乳：奶量/容量, 右乳：奶量/容量」。</summary>
    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForBreastHediff(this Pawn pawn, Hediff breastHediff)
    {
        var list = new List<(string key, float fullness, float capacity, bool isLeft)>();
        if (pawn?.CompEquallyMilkable() == null || breastHediff == null) return list;
        string key = pawn.GetPoolKeyForBreastHediff(breastHediff);
        if (string.IsNullOrEmpty(key)) return list;
        var comp = pawn.CompEquallyMilkable();
        foreach (var e in pawn.GetBreastPoolEntries())
        {
            if (e.Key != key && e.Key != key + "_L" && e.Key != key + "_R") continue;
            float f = comp.GetFullnessForKey(e.Key);
            list.Add((e.Key, f, e.Capacity, e.IsLeft));
        }
        return list;
    }

    /// <summary>获取“乳房/胸部”身体部位，用于将 hediff 挂在健康页的乳房行。优先 Breast，否则 Chest（RJW），否则 Torso。无合适部位时返回 null（hediff 将显示为全身）。</summary>
    public static BodyPartRecord GetBreastOrChestPart(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return null;
        var parts = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null);
        BodyPartRecord breast = null, chest = null, torso = null;
        foreach (var p in parts)
        {
            if (p?.def?.defName == null) continue;
            var dn = p.def.defName;
            if (dn.IndexOf("Breast", System.StringComparison.OrdinalIgnoreCase) >= 0) { breast = p; break; }
            if (dn == "Chest") chest = p;
            if (dn == "Torso") torso = p;
        }
        return breast ?? chest ?? torso;
    }

    /// <summary>获取“生殖器”身体部位，用于将精液相关 hediff 挂在健康页的生殖器行。优先使用 RJW Genital_Helper.get_genitalsBPR，否则从身体中找 Genitals 或 Anus。无合适部位时返回 null。</summary>
    public static BodyPartRecord GetGenitalsPart(this Pawn pawn)
    {
        if (pawn == null) return null;
        try
        {
            var bpr = rjw.Genital_Helper.get_genitalsBPR(pawn);
            if (bpr != null) return bpr;
        }
        catch { }
        if (pawn.health?.hediffSet == null) return null;
        var parts = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null);
        BodyPartRecord genitals = null, anus = null;
        foreach (var p in parts)
        {
            if (p?.def?.defName == null) continue;
            if (p.def.defName == "Genitals") genitals = p;
            if (p.def.defName == "Anus") anus = p;
        }
        return genitals ?? anus;
    }

    public static float DistanceTo(this Pawn pawn, Pawn other)
    {
        if (pawn.Map != other.Map || pawn.Map == null || other.Map == null) return float.MaxValue;
        return pawn.Position.DistanceTo(other.Position);
    }
    public static void Set(this CompProperties_Milkable comp, RaceMilkType value)
    {
        comp.milkDef = DefDatabase<ThingDef>.GetNamed(value.milkTypeDefName, true);
        comp.milkAmount = value.milkAmount;
        comp.milkIntervalDays = 1;
        comp.milkFemaleOnly = false;
    }
    public static CompEquallyMilkable CompEquallyMilkable(this ThingWithComps thing)
    {
        if (thing is not Pawn pawn) { return null; }
        CompEquallyMilkable comp = pawn.TryGetComp<CompEquallyMilkable>();
        if (comp == null)
        {
            comp = new CompEquallyMilkable
            {
                parent = pawn,
                props = pawn.def.GetCompProperties<CompProperties_Milkable>() ??
                    new CompProperties_Milkable()
                    {
                        milkDef = pawn.MilkDef() ?? DefDatabase<ThingDef>.GetNamed("Milk", true),
                        milkAmount = (int)pawn.MilkAmount(),
                        milkIntervalDays = 1,
                        milkFemaleOnly = false
                    }
            };
            pawn.AllComps.Add(comp);
        }
        return comp;
    }
    public static bool IsAdultFemaleAnimalOfColony(this Pawn pawn)
    {
        return pawn.Faction == Faction.OfPlayer
            && pawn.IsNormalAnimal()
            && !(pawn.LactatingHediff()?.Severity >= 1)
            && pawn.gender != Gender.Male
            && pawn.ageTracker.CurLifeStage?.reproductive == true;
    }
    #endregion
}
