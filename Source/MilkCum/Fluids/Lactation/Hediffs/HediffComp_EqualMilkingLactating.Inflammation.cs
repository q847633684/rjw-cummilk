using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
using MilkCum.Integration.DubsBadHygiene;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using rjw;

namespace MilkCum.Fluids.Lactation.Hediffs;

public partial class HediffComp_EqualMilkingLactating
{
    /// <summary>各侧 I 的最大值，供 L 衰减、品质等全局读取。</summary>
    public float CurrentInflammation => GetInflammationMax();

    /// <summary>指定池侧 key 的炎症 I（无记录为 0）。</summary>
    public float GetInflammationForKey(string sideKey)
    {
        if (string.IsNullOrEmpty(sideKey) || inflammationByKey == null) return 0f;
        return inflammationByKey.TryGetValue(sideKey, out float v) ? v : 0f;
    }

    /// <summary>炎症字典中各侧 I 的最大值。</summary>
    private float GetInflammationMax()
    {
        if (inflammationByKey == null || inflammationByKey.Count == 0) return 0f;
        float m = 0f;
        foreach (float v in inflammationByKey.Values)
            if (v > m) m = v;
        return m;
    }

    /// <summary>炎症/排空用池条目：与 <see cref="CompEquallyMilkable.GetResolvedBreastPoolEntries"/> 一致。</summary>
    private List<FluidPoolEntry> GetPoolEntriesForInflammation(CompEquallyMilkable comp)
    {
        if (comp != null)
            return comp.GetResolvedBreastPoolEntries();
        return Pawn?.GetBreastPoolEntries() ?? new List<FluidPoolEntry>();
    }

    private void SetInflammationForKeyInternal(string key, float value)
    {
        if (string.IsNullOrEmpty(key)) return;
        inflammationByKey ??= new Dictionary<string, float>();
        if (value <= 0f)
        {
            inflammationByKey.Remove(key);
            return;
        }
        inflammationByKey[key] = value;
    }

    private void PruneInflammationToValidKeys(HashSet<string> validKeys)
    {
        if (inflammationByKey == null || inflammationByKey.Count == 0) return;
        var keys = inflammationByKey.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
            if (!validKeys.Contains(keys[i]))
                inflammationByKey.Remove(keys[i]);
    }

    private void PruneLetdownToValidKeys(HashSet<string> validKeys)
    {
        if (letdownReflexByKey == null || letdownReflexByKey.Count == 0) return;
        var keys = letdownReflexByKey.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
            if (!validKeys.Contains(keys[i]))
                letdownReflexByKey.Remove(keys[i]);
    }

    /// <summary>
    /// 与 <see cref="CompEquallyMilkable"/> 乳池键迁移对齐：键集合不变时剔除孤儿键；变化时按旧键奶量加权平均 R/I 后写入各新键（无奶则对旧字典简单平均）。
    /// </summary>
    public void SyncPoolKeyedStateToEntries(IReadOnlyDictionary<string, float> preMigrateBreastFullness, List<FluidPoolEntry> newEntries)
    {
        var newKeySet = new HashSet<string>();
        if (newEntries != null)
        {
            for (int i = 0; i < newEntries.Count; i++)
            {
                string k = newEntries[i].Key;
                if (!string.IsNullOrEmpty(k)) newKeySet.Add(k);
            }
        }

        if (newKeySet.Count == 0)
        {
            letdownReflexByKey?.Clear();
            inflammationByKey?.Clear();
            return;
        }

        var oldMilkKeys = new HashSet<string>();
        if (preMigrateBreastFullness != null)
        {
            foreach (var k in preMigrateBreastFullness.Keys)
                if (!string.IsNullOrEmpty(k)) oldMilkKeys.Add(k);
        }

        if (oldMilkKeys.SetEquals(newKeySet))
        {
            PruneLetdownToValidKeys(newKeySet);
            PruneInflammationToValidKeys(newKeySet);
            return;
        }

        float totalMilk = 0f;
        if (preMigrateBreastFullness != null)
        {
            foreach (var v in preMigrateBreastFullness.Values)
                totalMilk += v;
        }

        float wLet = 0f;
        float wInfl = 0f;
        if (totalMilk > PoolModelConstants.Epsilon && preMigrateBreastFullness != null)
        {
            foreach (var kv in preMigrateBreastFullness)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                float frac = kv.Value / totalMilk;
                if (letdownReflexByKey != null && letdownReflexByKey.TryGetValue(kv.Key, out float r))
                    wLet += r * frac;
                if (inflammationByKey != null && inflammationByKey.TryGetValue(kv.Key, out float inf))
                    wInfl += inf * frac;
            }
        }
        else
        {
            if (letdownReflexByKey != null && letdownReflexByKey.Count > 0)
                wLet = letdownReflexByKey.Values.Average();
            if (inflammationByKey != null && inflammationByKey.Count > 0)
                wInfl = inflammationByKey.Values.Average();
        }

        letdownReflexByKey ??= new Dictionary<string, float>();
        letdownReflexByKey.Clear();
        inflammationByKey ??= new Dictionary<string, float>();
        inflammationByKey.Clear();

        foreach (string k in newKeySet)
        {
            letdownReflexByKey[k] = Mathf.Clamp01(wLet);
            if (wInfl > PoolModelConstants.Epsilon)
                SetInflammationForKeyInternal(k, wInfl);
        }
    }

    /// <summary>排空后按移出量/该侧撑大上限降低该侧 I。</summary>
    public void ApplyDrainInflammationRelief(Dictionary<string, float> drainedByKey, CompEquallyMilkable comp)
    {
        if (!MilkCumSettings.enableInflammationModel || drainedByKey == null || drainedByKey.Count == 0 || comp == null) return;
        float scale = Mathf.Max(0f, MilkCumSettings.inflammationDrainReliefScale);
        float maxDrop = Mathf.Max(0f, MilkCumSettings.inflammationDrainReliefMaxPerEvent);
        if (scale <= 0f && maxDrop <= 0f) return;
        var entries = GetPoolEntriesForInflammation(comp);
        foreach (var kv in drainedByKey)
        {
            if (kv.Value <= 0f || string.IsNullOrEmpty(kv.Key)) continue;
            float stretch = 0.01f;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Key != kv.Key) continue;
                stretch = Mathf.Max(0.001f, entries[i].Capacity * PoolModelConstants.StretchCapFactor);
                break;
            }
            float ratio = kv.Value / stretch;
            float drop = Mathf.Min(maxDrop, scale * ratio);
            if (drop <= 0f) continue;
            float cur = GetInflammationForKey(kv.Key);
            SetInflammationForKeyInternal(kv.Key, Mathf.Max(0f, cur - drop));
        }
    }

    /// <summary>按侧更新 I：淤积仅当 P 超阈值；卫生×淤积耦合；ρ·I 回落。Δt 小时。realismStasisTermScale 拟真 SYS-05 仅乘淤积项。</summary>
    public void UpdateInflammation(CompEquallyMilkable comp, float deltaTHours, float realismStasisTermScale = 1f)
    {
        if (!MilkCumSettings.enableInflammationModel || deltaTHours <= 0f || comp == null) return;
        var entries = GetPoolEntriesForInflammation(comp);
        var validKeys = new HashSet<string>();
        for (int i = 0; i < entries.Count; i++)
            if (!string.IsNullOrEmpty(entries[i].Key))
                validKeys.Add(entries[i].Key);
        PruneInflammationToValidKeys(validKeys);
        float alpha = Mathf.Max(0f, MilkCumSettings.inflammationAlpha);
        float beta = Mathf.Max(0f, MilkCumSettings.inflammationBeta);
        float gamma = Mathf.Max(0f, MilkCumSettings.inflammationGamma);
        float rho = Mathf.Max(0.001f, MilkCumSettings.inflammationRho);
        float stasisTh = Mathf.Clamp01(MilkCumSettings.inflammationStasisFullnessThreshold);
        float stasisExp = Mathf.Clamp(MilkCumSettings.inflammationStasisExponent, 1f, 4f);
        float hygieneBase = Mathf.Clamp01(MilkCumSettings.inflammationHygieneBaselineFactor);
        float denomHygiene = Mathf.Max(1e-4f, 1f - stasisTh);
        float injury = MilkRelatedHealthHelper.HasTorsoOrBreastInjury(Pawn) ? 1f : 0f;
        float badHygiene = Pawn != null ? DubsBadHygieneIntegration.GetHygieneRiskFactorForMastitis(Pawn) : 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            float stretch = Mathf.Max(0.001f, e.Capacity * PoolModelConstants.StretchCapFactor);
            float fk = comp.GetFullnessForKey(e.Key);
            float pk = fk / stretch;
            float excess = Mathf.Max(0f, pk - stasisTh);
            float stasisTerm = alpha * Mathf.Pow(excess, stasisExp) * Mathf.Max(0f, realismStasisTermScale);
            float normStasis = Mathf.Clamp01(excess / denomHygiene);
            float hygieneMult = Mathf.Lerp(hygieneBase, 1f, normStasis);
            float hygTerm = gamma * badHygiene * hygieneMult;
            float ik = GetInflammationForKey(e.Key);
            float dik = (stasisTerm + beta * injury + hygTerm - rho * ik) * deltaTHours;
            SetInflammationForKeyInternal(e.Key, Mathf.Max(0f, ik + dik));
        }
    }

    /// <summary>四层模型：若 I>I_crit 且允许乳腺炎，则�?EM_Mastitis（与现有 MTB 判定并存）。启用质量时有效 I_crit �?MilkQuality 提高</summary>
    public void TryTriggerMastitisFromInflammation()
    {
        if (!MilkCumSettings.enableInflammationModel || !MilkCumSettings.allowMastitis) return;
        if (MilkCumDefOf.EM_Mastitis == null || Pawn == null || !Pawn.RaceProps.Humanlike || !Pawn.IsLactating()) return;
        if (MilkCumDefOf.EM_BreastAbscess != null && Pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_BreastAbscess) != null) return;
        float crit = Mathf.Max(0.01f, MilkCumSettings.inflammationCrit);
        if (MilkCumSettings.enableMilkQuality)
            crit *= 1f + MilkCumSettings.milkQualityProtectionFactor * GetMilkQuality();
        if (GetInflammationMax() < crit) return;
        MilkRelatedHealthHelper.RemoveLactationalMilkStasis(Pawn);
        var existing = Pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_Mastitis);
        if (existing != null)
        {
            if (existing.Severity < 0.99f)
                existing.Severity = Mathf.Min(1f, existing.Severity + 0.1f);
        }
        else
            Pawn.health.AddHediff(MilkCumDefOf.EM_Mastitis, Pawn.GetBreastOrChestPart());
        float relief = crit * 0.5f;
        if (inflammationByKey == null || inflammationByKey.Count == 0) return;
        var inflKeys = inflammationByKey.Keys.ToList();
        for (int i = 0; i < inflKeys.Count; i++)
        {
            float v = GetInflammationForKey(inflKeys[i]);
            SetInflammationForKeyInternal(inflKeys[i], Mathf.Max(0f, v - relief));
        }
    }

    /// <summary>四层模型（阶�?.3）：MilkQuality = f(Hunger, I) �?[0,1]。饱食度高、炎症低则质量高；未启用质量时返�?1</summary>
    public float GetMilkQuality()
    {
        if (!MilkCumSettings.enableMilkQuality || Pawn == null) return 1f;
        float hunger = Pawn.needs?.food != null ? Mathf.Clamp01(Pawn.needs.food.CurLevel) : 1f;
        float w = Mathf.Clamp(MilkCumSettings.milkQualityInflammationWeight, 0f, 2f);
        float fromInflammation = Mathf.Clamp01(1f - w * GetInflammationMax());
        return Mathf.Clamp01(hunger * fromInflammation);
    }
}
