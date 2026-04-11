using System;
using System.Collections.Generic;
using MilkCum.Core.Constants;
using MilkCum.Fluids.Shared.Data;
using MilkCum.Integration.RjwBallsOvaries;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Cum.Common;

/// <summary>
/// RJW 阴茎/雄性产卵器与「虚拟左/右睾丸」两槽：每个 <see cref="Pawn"/> 自有存档字典；该小人身上全部可射精器官汇入双侧储备（<see cref="HediffComp_SexPart.GetFluidAmount"/> / 流速）；互斥左/右以外（含同时匹配左右）按模糊各半摊入两侧。
/// </summary>
public static class RjwSemenPoolEconomy
{
    public static readonly string TesticleLeftPoolKey = FluidSiteKind.TesticleLeft.ToString();
    public static readonly string TesticleRightPoolKey = FluidSiteKind.TesticleRight.ToString();

    public static bool IsPenisLikeSemenPart(ISexPartHediff p)
    {
        if (p?.Def == null) return false;
        GenitalFamily gf = p.Def.genitalFamily;
        if (gf != GenitalFamily.Penis && gf != GenitalFamily.MaleOvipositor) return false;
        if (p.Def.genitalTags == null || !p.Def.genitalTags.Contains(GenitalTag.CanPenetrate)) return false;
        HediffComp_SexPart comp = p.GetPartComp();
        return comp?.Fluid != null && comp.GetFluidAmount() > PoolModelConstants.Epsilon;
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

    /// <summary>每条可射精解剖行；<see cref="BuildSemenPoolEntries"/> 聚合为左/右两虚拟槽。</summary>
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
                bool exclusiveLeft = BodyPartLaterality.IsExclusiveLeft(bpr);
                result.Add(new RjwSemenPoolSideRow(idx++, h, cap, flow, exclusiveLeft));
            }
        }
        catch (Exception ex)
        {
            if (Prefs.DevMode) Log.Warning($"[MilkCum] GetSemenPoolSideRows: {ex.Message}");
            result.Clear();
        }

        return result;
    }

    /// <summary>至多两条虚拟睾丸槽（<see cref="FluidSiteKind.TesticleLeft"/> / <see cref="FluidSiteKind.TesticleRight"/>）；多阴茎汇入左右或模糊对半。</summary>
    public static List<FluidPoolEntry> BuildSemenPoolEntries(Pawn pawn)
    {
        var result = new List<FluidPoolEntry>();
        if (pawn == null) return result;
        var rows = GetSemenPoolSideRows(pawn);
        if (rows.Count == 0) return result;
        try
        {
            float leftCap = 0f, rightCap = 0f, ambCap = 0f;
            float leftFlowWeighted = 0f, rightFlowWeighted = 0f, ambFlowWeighted = 0f;
            BodyPartRecord leftPart = null, rightPart = null, ambPart = null;
            for (int i = 0; i < rows.Count; i++)
            {
                RjwSemenPoolSideRow r = rows[i];
                BodyPartRecord part = r.GenitalHediff?.Part;
                if (BodyPartLaterality.IsExclusiveLeft(part))
                {
                    leftCap += r.NominalFluidAmount;
                    leftFlowWeighted += r.FlowMultiplier * r.NominalFluidAmount;
                    leftPart ??= part;
                }
                else if (BodyPartLaterality.IsExclusiveRight(part))
                {
                    rightCap += r.NominalFluidAmount;
                    rightFlowWeighted += r.FlowMultiplier * r.NominalFluidAmount;
                    rightPart ??= part;
                }
                else
                {
                    ambCap += r.NominalFluidAmount;
                    ambFlowWeighted += r.FlowMultiplier * r.NominalFluidAmount;
                    ambPart ??= part;
                }
            }

            leftCap += ambCap * 0.5f;
            rightCap += ambCap * 0.5f;
            leftFlowWeighted += ambFlowWeighted * 0.5f;
            rightFlowWeighted += ambFlowWeighted * 0.5f;
            float ballzCapMul = RjwBallsOvariesIntegration.GetVirtualSemenCapacityMultiplier(pawn);
            leftCap *= ballzCapMul;
            rightCap *= ballzCapMul;
            leftFlowWeighted *= ballzCapMul;
            rightFlowWeighted *= ballzCapMul;
            float leftFlow = leftCap > PoolModelConstants.Epsilon ? leftFlowWeighted / leftCap : 1f;
            float rightFlow = rightCap > PoolModelConstants.Epsilon ? rightFlowWeighted / rightCap : 1f;
            int poolIdx = 0;
            if (leftCap > PoolModelConstants.Epsilon)
            {
                result.Add(new FluidPoolEntry(
                    FluidSiteKind.TesticleLeft,
                    leftCap,
                    leftFlow,
                    isLeft: true,
                    poolIndex: poolIdx++,
                    sourcePart: leftPart ?? ambPart));
            }

            if (rightCap > PoolModelConstants.Epsilon)
            {
                result.Add(new FluidPoolEntry(
                    FluidSiteKind.TesticleRight,
                    rightCap,
                    rightFlow,
                    isLeft: false,
                    poolIndex: poolIdx,
                    sourcePart: rightPart ?? ambPart));
            }
        }
        catch (Exception ex)
        {
            if (Prefs.DevMode) Log.Warning($"[MilkCum] BuildSemenPoolEntries: {ex.Message}");
            result.Clear();
        }

        return result;
    }

    /// <summary>该射精部位扣池键：解剖左/右只扣一侧；未标注则两侧按当前存量比例分担。</summary>
    public static void AddVirtualSemenKeysForPart(ISexPartHediff part, List<string> dest)
    {
        if (dest == null || part == null) return;
        BodyPartRecord bpr = part.AsHediff?.Part;
        if (BodyPartLaterality.IsExclusiveLeft(bpr))
        {
            dest.Add(TesticleLeftPoolKey);
            return;
        }

        if (BodyPartLaterality.IsExclusiveRight(bpr))
        {
            dest.Add(TesticleRightPoolKey);
            return;
        }

        dest.Add(TesticleLeftPoolKey);
        dest.Add(TesticleRightPoolKey);
    }
}

/// <summary>一条可射精生殖器解剖行。</summary>
public readonly struct RjwSemenPoolSideRow
{
    public int PoolIndex { get; }
    public Hediff GenitalHediff { get; }
    public float NominalFluidAmount { get; }
    public float FlowMultiplier { get; }
    /// <summary>解剖上独占左侧（<c>左</c> 且不同时为右侧），与汇聚时的左桶一致。</summary>
    public bool IsLeft { get; }

    public RjwSemenPoolSideRow(int poolIndex, Hediff genitalHediff, float nominalFluidAmount, float flowMultiplier, bool exclusiveLeft)
    {
        PoolIndex = poolIndex;
        GenitalHediff = genitalHediff;
        NominalFluidAmount = nominalFluidAmount;
        FlowMultiplier = flowMultiplier;
        IsLeft = exclusiveLeft;
    }
}
