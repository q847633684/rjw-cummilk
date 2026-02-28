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
    /// <summary>规格：种族×乳房大小按部位区分。左乳容量占比（0～1），与右乳合计 1；无 RJW 或未启用时 0.5/0.5。</summary>
    public static float GetLeftBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out float left, out _);
        return left;
    }
    /// <summary>右乳容量占比（0～1），与左乳合计 1。</summary>
    public static float GetRightBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out _, out float right);
        return right;
    }
    /// <summary>按 RJW 乳房 Hediff 列表与 Severity（大小）计算左右容量占比；列表 [0]=左、[1]=右，单乳时全归左。</summary>
    private static void GetBreastCapacityFactors(Pawn pawn, out float leftFactor, out float rightFactor)
    {
        leftFactor = 0.5f;
        rightFactor = 0.5f;
        if (pawn == null || !EqualMilkingSettings.rjwBreastSizeEnabled || !pawn.RaceProps.Humanlike)
            return;
        try
        {
            var list = pawn.GetBreastList();
            if (list == null || list.Count == 0) return;
            if (list.Count == 1)
            {
                leftFactor = 1f;
                rightFactor = 0f;
                return;
            }
            float leftSize = Mathf.Clamp(list[0].Severity, 0.01f, 10f);
            float rightSize = Mathf.Clamp(list[1].Severity, 0.01f, 10f);
            float total = leftSize + rightSize;
            leftFactor = leftSize / total;
            rightFactor = rightSize / total;
        }
        catch
        {
            // RJW 未加载或 GetBreastList 异常时保持 0.5/0.5
        }
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
