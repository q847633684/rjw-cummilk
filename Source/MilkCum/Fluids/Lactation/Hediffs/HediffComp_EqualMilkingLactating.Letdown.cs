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

    /// <summary>四层模型：挤奶/吸奶时 L 微幅刺激，单次与每日带上限；仅 enableInflammationModel 时生效。</summary>
    public void AddMilkingLStimulus() => AddMilkingLStimulus(MilkingStimulusSource.Generic);

    /// <inheritdoc cref="AddMilkingLStimulus()"/>
    public void AddMilkingLStimulus(MilkingStimulusSource stimulusSource)
    {
        float perEvent = Mathf.Clamp(MilkCumSettings.milkingLStimulusPerEvent, 0f, 1f);
        perEvent *= MilkRealismHelper.GetMilkingStimulusMultiplier(stimulusSource);
        float capEvent = Mathf.Clamp(MilkCumSettings.milkingLStimulusCapPerEvent, 0f, 1f);
        float capDay = Mathf.Clamp(MilkCumSettings.milkingLStimulusCapPerDay, 0f, 1f);
        int now = Find.TickManager.TicksGame;
        if (lastMilkingLStimulusDayTick < 0 || now - lastMilkingLStimulusDayTick >= 60000)
        {
            milkingLStimulusAccumulatedThisDay = 0f;
            lastMilkingLStimulusDayTick = now;
        }
        float room = Mathf.Max(0f, capDay - milkingLStimulusAccumulatedThisDay);
        float toAdd = Mathf.Min(perEvent, capEvent, room);
        if (toAdd <= 0f) return;
        lactationAmountFromDrug += toAdd;
        milkingLStimulusAccumulatedThisDay += toAdd;
    }

    /// <summary>四层模型：当前喷乳反射 R∈[0,1]（用于总览显示）。未启用时返回 1；启用时为各侧 R 的算术平均，无数据时返回 0。</summary>
    public float GetLetdownReflex()
    {
        if (!MilkCumSettings.enableLetdownReflex) return 1f;
        var entries = Pawn?.GetResolvedBreastPoolEntries();
        if ((entries?.Count ?? 0) == 0) return 0f;
        float sumR = 0f;
        int n = 0;
        foreach (var e in entries)
        {
            sumR += GetLetdownReflexForSide(e.Key);
            n++;
        }
        return n > 0 ? Mathf.Clamp01(sumR / n) : 0f;
    }

    /// <summary>指定池 key 的 R 原始值；无记录时返回 0。</summary>
    private float GetLetdownReflexRaw(string sideKey)
    {
        if (string.IsNullOrEmpty(sideKey) || letdownReflexByKey == null || !letdownReflexByKey.TryGetValue(sideKey, out float r))
            return 0f;
        return Mathf.Clamp01(r);
    }

    /// <summary>指定池键的 R（公开给 UI），无记录时为 0；<see cref="GetLetdownReflex"/> 对各键算术平均用于总览。</summary>
    public float GetLetdownReflexForSide(string sideKey)
    {
        if (!MilkCumSettings.enableLetdownReflex) return 1f;
        return GetLetdownReflexRaw(sideKey);
    }

    /// <summary>进水流速的 R 倍率（按侧）：无刺激（该侧无记录）时=1；有刺激时倍率 = 1+R×(boost−1)，R 可衰减到 0 时倍率回 1。</summary>
    public float GetLetdownReflexFlowMultiplier(string sideKey)
    {
        if (!MilkCumSettings.enableLetdownReflex) return 1f;
        if (string.IsNullOrEmpty(sideKey) || letdownReflexByKey == null || !letdownReflexByKey.TryGetValue(sideKey, out float rVal))
            return 1f;
        float r = Mathf.Clamp01(rVal);
        float boost = Mathf.Clamp(MilkCumSettings.letdownReflexBoostMultiplier, 1f, 3f);
        return Mathf.Max(1f, 1f + r * (boost - 1f));
    }

    /// <summary>进水流速的 R 倍率（全侧平均，用于总产奶效率一行显示）</summary>
    public float GetLetdownReflexFlowMultiplier()
    {
        if (!MilkCumSettings.enableLetdownReflex) return 1f;
        var entries = Pawn?.GetResolvedBreastPoolEntries();
        if ((entries?.Count ?? 0) == 0) return 1f;
        float sum = 0f;
        foreach (var e in entries)
            sum += GetLetdownReflexFlowMultiplier(e.Key);
        return entries.Count > 0 ? sum / entries.Count : 1f;
    }

    /// <summary>四层模型：各侧 R 指数衰减，可衰减到 0；仅保留当前池中存在的 key，避免字典无限增长。</summary>
    public void DecayLetdownReflex(float deltaTMinutes)
    {
        if (!MilkCumSettings.enableLetdownReflex || deltaTMinutes <= 0f) return;
        if (letdownReflexByKey == null || letdownReflexByKey.Count == 0) return;
        float lambda = Mathf.Max(0.001f, MilkCumSettings.letdownReflexDecayLambda);
        var entries = Pawn?.GetResolvedBreastPoolEntries();
        var validKeys = entries?.Select(e => e.Key).ToHashSet();
        var keys = letdownReflexByKey.Keys.ToList();
        var toRemove = new List<string>();
        foreach (string key in keys)
        {
            if (validKeys != null && !validKeys.Contains(key))
            {
                toRemove.Add(key);
                continue;
            }
            float r = letdownReflexByKey[key] * Mathf.Exp(-lambda * deltaTMinutes);
            if (r < 1E-5f) r = 0f;
            letdownReflexByKey[key] = r;
        }
        foreach (string key in toRemove)
            letdownReflexByKey.Remove(key);
    }

    /// <summary>四层模型：挤奶/吸奶刺激该侧，R += ΔR，Clamp 到 1。仅被刺激的乳池侧提升喷乳反射。</summary>
    public void AddLetdownReflexStimulus(string sideKey)
    {
        if (!MilkCumSettings.enableLetdownReflex || string.IsNullOrEmpty(sideKey)) return;
        letdownReflexByKey ??= new Dictionary<string, float>();
        float deltaR = Mathf.Clamp(MilkCumSettings.letdownReflexStimulusDeltaR, 0f, 1f);
        float r = GetLetdownReflexRaw(sideKey);
        letdownReflexByKey[sideKey] = Mathf.Min(1f, r + deltaR);
    }
}
