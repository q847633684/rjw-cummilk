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

    /// <summary>分娩后首次泌乳成就类记忆；仅当尚未拥有该记忆时发放，供原版/RJW 分娩入口统一调用。</summary>
    public static void TryGiveFirstLactationBirthMemory(Pawn mother)
    {
        if (mother == null || EMDefOf.EM_FirstLactationBirth == null || mother.needs?.mood?.thoughts?.memories == null) return;
        if (mother.needs.mood.thoughts.memories.Memories.Any(m => m.def == EMDefOf.EM_FirstLactationBirth)) return;
        mother.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_FirstLactationBirth);
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
    /// <summary>Part.def.defName 是否为左乳（StartsWith("LeftBreast") 或 "Left"），避免 Contains("Left") 误伤 LeftoverBreast。</summary>
    private static bool IsLeftPart(string partName) => !string.IsNullOrEmpty(partName) && (partName.StartsWith("LeftBreast") || partName == "Left");
    /// <summary>Part.def.defName 是否为右乳（StartsWith("RightBreast") 或 "Right"）。</summary>
    private static bool IsRightPart(string partName) => !string.IsNullOrEmpty(partName) && (partName.StartsWith("RightBreast") || partName == "Right");

    /// <summary>虚拟左右池容量（仅计算层，不修改任何 Hediff）。每个 hediff 表示一对乳房：左/右各为该 hediff 的 Severity×系数；多 hediff 时左右均为所有 hediff 容量之和（总容量=2×和）。容量允许 0（乳退化）。</summary>
    public static (float leftCapacity, float rightCapacity) GetVirtualBreastPools(this Pawn pawn)
    {
        if (pawn == null) return (0f, 0f);
        var list = pawn.GetBreastList();
        if (list == null || list.Count == 0) return (0f, 0f);
        float coeff = EqualMilkingSettings.rjwBreastCapacityCoefficient;
        float totalCap = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            var h = list[i];
            if (h?.def == null) continue;
            totalCap += Mathf.Clamp(h.Severity * coeff, 0f, 10f);
        }
        return (totalCap, totalCap);
    }

    /// <summary>左池容量：虚拟左池，不修改 Hediff。</summary>
    public static float GetLeftBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out float left, out _);
        return left;
    }
    /// <summary>右池容量：虚拟右池，不修改 Hediff。</summary>
    public static float GetRightBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out _, out float right);
        return right;
    }
    /// <summary>左池/右池容量。仅计算层虚拟左右池：未启用 RJW 乳房尺寸时人形 0.5+0.5；否则每个 hediff 表示一对乳房，左右均为所有 hediff 容量之和（总容量=2×和）。不调用 AddHediff/RemoveHediff。</summary>
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
        var list = pawn.GetBreastList();
        if (list == null || list.Count == 0)
        {
            if (pawn.RaceProps?.Humanlike == true) { leftFactor = 0.5f; rightFactor = 0.5f; }
            return;
        }
        float coeff = EqualMilkingSettings.rjwBreastCapacityCoefficient;
        try
        {
            float totalCap = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (h?.def == null) continue;
                totalCap += Mathf.Clamp(h.Severity * coeff, 0f, 10f);
            }
            if (totalCap >= 0.01f)
            {
                leftFactor = totalCap;
                rightFactor = totalCap;
                return;
            }
            if (pawn.RaceProps?.Humanlike == true) { leftFactor = 0.5f; rightFactor = 0.5f; }
        }
        catch
        {
            if (pawn.RaceProps?.Humanlike == true) { leftFactor = 0.5f; rightFactor = 0.5f; }
        }
    }

    /// <summary>按单乳枚举池条目（虚拟左右池，不修改 Hediff）。每个 hediff 表示一对乳房：产生 _L/_R 两键，容量各为该 hediff 的 Severity×系数。同一对共享 PairIndex。见 记忆库/design/双池与PairIndex。</summary>
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
            var list = pawn.GetBreastList();
            if (list == null || list.Count == 0)
            {
                if (pawn.RaceProps?.Humanlike == true)
                {
                    result.Add(new BreastPoolEntry("Left_Default", 0.5f, 0.5f, true, 0));
                    result.Add(new BreastPoolEntry("Right_Default", 0.5f, 0.5f, false, 0));
                }
                return result;
            }
            float coeff = EqualMilkingSettings.rjwBreastCapacityCoefficient;
            int currentPair = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (h?.def == null) continue;
                string partName = h.Part?.def?.defName;
                string key = !string.IsNullOrEmpty(partName) ? partName : h.def.defName + "_" + i;
                float cap = Mathf.Clamp(h.Severity * coeff, 0f, 10f);
                float mult = (h.def is HediffDef_SexPart d) ? Mathf.Clamp(d.fluidMultiplier, 0.1f, 3f) : 1f;
                result.Add(new BreastPoolEntry(key + "_L", cap, mult, true, currentPair));
                result.Add(new BreastPoolEntry(key + "_R", cap, mult, false, currentPair));
                currentPair++;
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
    public static float MilkMarketValue(this Pawn pawn) => pawn.MilkDef()?.BaseMarketValue ?? 0;
    public static bool MilkTypeCanBreastfeed(this Pawn mom) => EqualMilkingSettings.MilkTypeCanBreastfeed(mom);
    public static bool CanBreastfeedEver(this Pawn mom, Pawn baby) => EqualMilkingSettings.CanBreastfeedEver(mom, baby);
    public static HediffComp_EqualMilkingLactating LactatingHediffComp(this Pawn pawn) => pawn.health.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating)?.TryGetComp<HediffComp_EqualMilkingLactating>();
    public static Hediff LactatingHediff(this Pawn pawn) => pawn.health.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating);
    public static HediffWithComps_EqualMilkingLactating LactatingHediffWithComps(this Pawn pawn) => pawn.LactatingHediff() as HediffWithComps_EqualMilkingLactating;

    /// <summary>规格：乳腺炎/堵塞等健康影响进水流速。返回 1f 减去乳房不适类 hediff 的惩罚（severity×0.5），最小 0.5。part 为 null 时按全身（任一乳腺炎即生效）；part 非空时仅当该部位的乳腺炎生效，对应「哪对乳房的哪一侧」。</summary>
    public static float GetMilkFlowMultiplierFromConditions(this Pawn pawn, BodyPartRecord part = null)
    {
        if (pawn?.health?.hediffSet == null) return 1f;
        if (global::MilkCum.Core.EMDefOf.EM_Mastitis == null) return 1f;
        var mastitis = pawn.health.hediffSet.hediffs.Where(x => x.def == global::MilkCum.Core.EMDefOf.EM_Mastitis);
        Hediff h = part == null ? mastitis.FirstOrDefault() : mastitis.FirstOrDefault(m => m.Part == part);
        if (h == null) return 1f;
        float penalty = h.Severity * 0.5f;
        return Mathf.Clamp(1f - penalty, 0.5f, 1f);
    }

    /// <summary>从侧 key（如 HumanBreast_L）得到池 key（HumanBreast）。</summary>
    public static string GetPoolKeyFromSideKey(string sideKey)
    {
        if (string.IsNullOrEmpty(sideKey)) return sideKey;
        if (sideKey.EndsWith("_L") || sideKey.EndsWith("_R"))
            return sideKey.Substring(0, sideKey.Length - 2);
        return sideKey;
    }

    /// <summary>该对乳房（poolKey）对应的身体部位，用于按部位的状态（如乳腺炎）判定。无对应 hediff 时返回 null。</summary>
    public static BodyPartRecord GetPartForPoolKey(this Pawn pawn, string poolKey)
    {
        if (pawn == null || string.IsNullOrEmpty(poolKey)) return null;
        var list = pawn.GetBreastList();
        if (list == null) return null;
        foreach (var h in list)
        {
            if (GetPoolKeyForBreastHediff(pawn, h) == poolKey)
                return h.Part;
        }
        return null;
    }

    /// <summary>指定侧（sideKey，如 poolKey_L）的状态系数，用于流速与 UI 按「该乳房该侧」显示。</summary>
    public static float GetConditionsForSide(this Pawn pawn, string sideKey)
    {
        if (pawn == null || string.IsNullOrEmpty(sideKey)) return 1f;
        string poolKey = GetPoolKeyFromSideKey(sideKey);
        BodyPartRecord part = pawn.GetPartForPoolKey(poolKey);
        return pawn.GetMilkFlowMultiplierFromConditions(part);
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

    /// <summary>左乳流速倍率：与容量一致，每个 hediff 表示一对，所有 hediff 的 fluidMultiplier 之和计入左。未启用 RJW 乳房尺寸时 0.5。</summary>
    public static float GetMilkFlowMultiplierFromRJW_Left(this Pawn pawn)
    {
        pawn.GetMilkFlowMultipliersFromRJW(out float left, out _);
        return left;
    }
    /// <summary>右乳流速倍率：与容量一致，每个 hediff 表示一对，所有 hediff 的 fluidMultiplier 之和计入右。未启用时 0.5。</summary>
    public static float GetMilkFlowMultiplierFromRJW_Right(this Pawn pawn)
    {
        pawn.GetMilkFlowMultipliersFromRJW(out _, out float right);
        return right;
    }

    /// <summary>左右池流速倍率（与容量一致：每个 hediff 表示一对，左/右均为所有 hediff 的 fluidMultiplier 之和）。未启用或空列表时 0.5/0.5。</summary>
    public static void GetMilkFlowMultipliersFromRJW(this Pawn pawn, out float leftMultiplier, out float rightMultiplier)
    {
        leftMultiplier = 0.5f;
        rightMultiplier = 0.5f;
        if (pawn == null || !EqualMilkingSettings.rjwBreastSizeEnabled) return;
        try
        {
            var list = pawn.GetBreastList();
            if (list == null || list.Count == 0) return;
            float totalMult = 0f;
            foreach (var h in list)
            {
                if (h?.def == null) continue;
                float mult = (h.def is HediffDef_SexPart d) ? Mathf.Clamp(d.fluidMultiplier, 0.1f, 3f) : 1f;
                totalMult += mult;
            }
            if (totalMult > 0f)
            {
                leftMultiplier = totalMult;
                rightMultiplier = totalMult;
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

    /// <summary>该对乳房的左右乳产奶流速（池单位/天）及各自流速倍率；流速按侧含压力与喷乳反射。单次遍历条目。</summary>
    public static (float flowLeft, float flowRight, float multLeft, float multRight) GetFlowPerDayForBreastPair(this Pawn pawn, string poolKey)
    {
        if (pawn == null || string.IsNullOrEmpty(poolKey)) return (0f, 0f, 0f, 0f);
        var comp = pawn.LactatingHediffComp();
        if (comp == null) return (0f, 0f, 0f, 0f);
        var milkComp = pawn.CompEquallyMilkable();
        var b = comp.GetFlowPerDayBreakdown();
        if (b.RjwSum < 0.001f) return (0f, 0f, 0f, 0f);
        var entries = pawn.GetBreastPoolEntries();
        float sumWeighted = 0f;
        float weightL = 0f, weightR = 0f, multL = 0f, multR = 0f;
        foreach (var e in entries)
        {
            float conditionsE = pawn.GetConditionsForSide(e.Key);
            float pressureE = 1f;
            if (milkComp != null)
            {
                float stretch = e.Capacity * PoolModelConstants.StretchCapFactor;
                float fullE = milkComp.GetFullnessForKey(e.Key);
                pressureE = EqualMilkingSettings.enablePressureFactor
                    ? EqualMilkingSettings.GetPressureFactor(fullE / Mathf.Max(0.001f, stretch))
                    : (fullE >= stretch ? 0f : 1f);
            }
            float weight = conditionsE * e.FlowMultiplier * pressureE * comp.GetLetdownReflexFlowMultiplier(e.Key);
            sumWeighted += weight;
            if (e.Key == poolKey + "_L") { weightL = weight; multL = e.FlowMultiplier; }
            else if (e.Key == poolKey + "_R") { weightR = weight; multR = e.FlowMultiplier; }
        }
        if (sumWeighted < 1E-5f) return (0f, 0f, 0f, 0f);
        float flowL = b.TotalFlow * weightL / sumWeighted;
        float flowR = b.TotalFlow * weightR / sumWeighted;
        return (flowL, flowR, multL, multR);
    }

    /// <summary>指定侧（poolKey_L / poolKey_R）的压力系数，用于健康页因子行按侧显示。P = 该侧满度/该侧撑大容量。</summary>
    public static float GetPressureFactorForSide(this Pawn pawn, string sideKey)
    {
        var milkComp = pawn?.CompEquallyMilkable();
        if (milkComp == null || string.IsNullOrEmpty(sideKey)) return 1f;
        var entries = pawn.GetBreastPoolEntries();
        if (entries == null) return 1f;
        var e = entries.FirstOrDefault(x => x.Key == sideKey);
        if (string.IsNullOrEmpty(e.Key)) return 1f;
        float stretch = e.Capacity * PoolModelConstants.StretchCapFactor;
        float fullE = milkComp.GetFullnessForKey(sideKey);
        return EqualMilkingSettings.enablePressureFactor
            ? EqualMilkingSettings.GetPressureFactor(fullE / Mathf.Max(0.001f, stretch))
            : (fullE >= stretch ? 0f : 1f);
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
