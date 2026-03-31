using System;
using System.Collections.Generic;
using MilkCum.Core.Constants;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Cum.Common;

/// <summary>
/// RJW 阴茎/雄性产卵器：每条可射精器官独立虚拟精池（键 <see cref="MakeSemenPartPoolKey"/>），容量与流速取该条 <see cref="HediffComp_SexPart.GetFluidAmount"/> / <see cref="GetSemenPartFlowMultiplier"/>。
/// </summary>
public static class RjwSemenPoolEconomy
{
    public const string SemenPoolKeyPrefix = "Semen_";

    /// <summary>单条射精 Hediff 的稳定池键（沿用存档 loadID，与乳叶路径键策略一致）。</summary>
    public static string MakeSemenPartPoolKey(Hediff h)
    {
        if (h == null) return "";
        return SemenPoolKeyPrefix + h.loadID;
    }

    public static bool IsPenisLikeSemenPart(ISexPartHediff p)
    {
        if (p?.Def == null) return false;
        GenitalFamily gf = p.Def.genitalFamily;
        if (gf != GenitalFamily.Penis && gf != GenitalFamily.MaleOvipositor) return false;
        if (p.Def.genitalTags == null || !p.Def.genitalTags.Contains(GenitalTag.CanPenetrate)) return false;
        HediffComp_SexPart comp = p.GetPartComp();
        return comp?.Fluid != null && comp.GetFluidAmount() > PoolModelConstants.Epsilon;
    }

    public static bool PartNameLooksLeft(BodyPartRecord part)
    {
        if (part == null) return false;
        if (!string.IsNullOrEmpty(part.customLabel))
        {
            if (part.customLabel.Contains("左")) return true;
            if (part.customLabel.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        if (part.def?.defName == null) return false;
        return part.def.defName.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool PartNameLooksRight(BodyPartRecord part)
    {
        if (part == null) return false;
        if (!string.IsNullOrEmpty(part.customLabel))
        {
            if (part.customLabel.Contains("右")) return true;
            if (part.customLabel.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        if (part.def?.defName == null) return false;
        return part.def.defName.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static float GetSemenPartFlowMultiplier(Hediff h)
    {
        if (h?.def is not HediffDef_SexPart d) return 1f;
        if (h.TryGetComp<HediffComp_SexPart>(out var comp))
            return Mathf.Max(0.01f, comp.GetFluidMultiplier());
        if (h.pawn != null)
        {
            float sev = h.Severity;
            return Mathf.Max(0.01f, d.GetFluidMultiplier(sev, 1f, h.pawn.BodySize, SexUtility.ScaleToHumanAge(h.pawn)));
        }

        return Mathf.Max(0.01f, d.fluidMultiplier);
    }

    /// <summary>每条可射精解剖行（<see cref="BuildSemenPoolEntries"/> 每条阴茎一行池条目）。</summary>
    public static List<RjwSemenPoolSideRow> GetSemenPoolSideRows(Pawn pawn)
    {
        var result = new List<RjwSemenPoolSideRow>();
        if (pawn == null) return result;
        try
        {
            int idx = 0;
            foreach (ISexPartHediff part in FluidUtility.GetGenitalsWithFluids(pawn))
            {
                if (!IsPenisLikeSemenPart(part)) continue;
                Hediff h = part.AsHediff;
                if (h == null) continue;
                BodyPartRecord bpr = h.Part;
                float cap = part.GetPartComp().GetFluidAmount();
                if (cap <= PoolModelConstants.Epsilon) continue;
                float flow = GetSemenPartFlowMultiplier(h);
                bool isLeft = PartNameLooksLeft(bpr);
                result.Add(new RjwSemenPoolSideRow(idx++, h, cap, flow, isLeft));
            }
        }
        catch (Exception ex)
        {
            if (Prefs.DevMode) Log.Warning($"[MilkCum] GetSemenPoolSideRows: {ex.Message}");
            result.Clear();
        }

        return result;
    }

    /// <summary>每条可射精生殖器一条 <see cref="FluidPoolEntry"/>（<see cref="FluidSiteKind.None"/> + 自定义键）；多阴茎互不合并。</summary>
    public static List<FluidPoolEntry> BuildSemenPoolEntries(Pawn pawn)
    {
        var result = new List<FluidPoolEntry>();
        if (pawn == null) return result;
        var rows = GetSemenPoolSideRows(pawn);
        if (rows.Count == 0) return result;
        try
        {
            int poolIdx = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                RjwSemenPoolSideRow r = rows[i];
                Hediff h = r.GenitalHediff;
                if (h == null) continue;
                string key = MakeSemenPartPoolKey(h);
                if (string.IsNullOrEmpty(key)) continue;
                BodyPartRecord partRec = h.Part;
                bool anatomicalLeft = PartNameLooksLeft(partRec);
                result.Add(new FluidPoolEntry(
                    key,
                    FluidSiteKind.None,
                    r.NominalFluidAmount,
                    r.FlowMultiplier,
                    anatomicalLeft,
                    poolIdx++,
                    partRec));
            }
        }
        catch (Exception ex)
        {
            if (Prefs.DevMode) Log.Warning($"[MilkCum] BuildSemenPoolEntries: {ex.Message}");
            result.Clear();
        }

        return result;
    }

    /// <summary>该射精部位对应的虚拟精池键（与 <see cref="BuildSemenPoolEntries"/> 一致，每器官单键）。</summary>
    public static void AddVirtualSemenKeysForPart(ISexPartHediff part, List<string> dest)
    {
        if (dest == null || part == null) return;
        string k = MakeSemenPartPoolKey(part.AsHediff);
        if (!string.IsNullOrEmpty(k)) dest.Add(k);
    }
}

/// <summary>一条可射精生殖器解剖行。</summary>
public readonly struct RjwSemenPoolSideRow
{
    public int PoolIndex { get; }
    public Hediff GenitalHediff { get; }
    public float NominalFluidAmount { get; }
    public float FlowMultiplier { get; }
    public bool IsLeft { get; }

    public RjwSemenPoolSideRow(int poolIndex, Hediff genitalHediff, float nominalFluidAmount, float flowMultiplier, bool isLeft)
    {
        PoolIndex = poolIndex;
        GenitalHediff = genitalHediff;
        NominalFluidAmount = nominalFluidAmount;
        FlowMultiplier = flowMultiplier;
        IsLeft = isLeft;
    }
}
