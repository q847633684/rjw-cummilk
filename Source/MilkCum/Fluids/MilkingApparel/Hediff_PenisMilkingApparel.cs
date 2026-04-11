using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Cum.Common;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.MilkingApparel;

/// <summary>
/// 阴茎穿戴式挤奶：每 tick 按与 <see cref="MilkCumDefOf.EM_MilkingElectric"/> 相同的基准流速从虚拟精池扣量（满度平方压）；关闭虚拟池时单根阴茎每 tick 不超过「旧版日脉冲量 / 每日 tick」以免爆炸。
/// 性满足改为约每游戏小时、且精池有压时触发一次。
/// </summary>
public class Hediff_PenisMilkingApparel : HediffWithComps
{
    private Dictionary<int, float> pendingFluidByPartLoadId = new();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref pendingFluidByPartLoadId, "EM.pendSemWear", LookMode.Value, LookMode.Value);
        pendingFluidByPartLoadId ??= new Dictionary<int, float>();
    }

    public override void Tick()
    {
        base.Tick();
        Pawn p = pawn;
        if (p == null || !p.Spawned || p.Dead) return;
        if (!(p.IsColonist || p.IsPrisoner || p.IsSlave || p.IsAnimal())) return;

        BodyPartRecord genitalsBpr = Genital_Helper.get_genitalsBPR(p);
        List<Hediff> partHediffs = Genital_Helper.get_PartsHediffList(p, genitalsBpr).Where(Genital_Helper.is_penis).ToList();
        if (partHediffs.NullOrEmpty()) return;

        float flowPerSecond = MilkingApparelFlowUtility.GetPenisMilkingFlowPerGameSecond(p);
        float ratePerTickTotal = flowPerSecond / MilkingApparelFlowUtility.TicksPerGameSecond;
        int n = 0;
        for (int i = 0; i < partHediffs.Count; i++)
        {
            if (partHediffs[i] is ISexPartHediff ph && ph.Def is HediffDef_SexPart pd && pd.IsNaturalSexPart() && pd.produceFluidOnOrgasm && ph.GetPartComp()?.Fluid != null)
                n++;
        }

        if (n <= 0) return;
        float slicePerPart = ratePerTickTotal / n;

        for (int i = 0; i < partHediffs.Count; i++)
        {
            Hediff h = partHediffs[i];
            if (h is not ISexPartHediff partHediff || partHediff.Def is not HediffDef_SexPart penisDef) continue;
            if (!penisDef.IsNaturalSexPart() || !penisDef.produceFluidOnOrgasm) continue;
            HediffComp_SexPart comp = partHediff.GetPartComp();
            if (comp?.Fluid == null) continue;

            float want = comp.FluidAmount * comp.FluidMultiplier;
            if (want <= 0f) continue;

            float nominal = slicePerPart;
            if (!MilkCumSettings.Cum_EnableVirtualSemenPool)
                nominal = Mathf.Min(slicePerPart, want / Mathf.Max(1, GenDate.TicksPerDay));

            float actual = p.ConsumeSemenForEjection(partHediff, nominal);
            if (actual <= 0f) continue;

            if (!MilkingApparelProductUtility.TryResolvePenisMilkingProduct(partHediff, out ThingDef product, out float perUnit))
                continue;

            int id = h.loadID;
            pendingFluidByPartLoadId.TryGetValue(id, out float acc);
            acc += actual;
            int whole = Mathf.FloorToInt(acc / perUnit);
            if (whole > 0)
            {
                MilkingApparelProductUtility.PlaceProductNear(p, product, whole);
                acc -= whole * perUnit;
            }

            if (acc < 1e-4f)
                pendingFluidByPartLoadId.Remove(id);
            else
                pendingFluidByPartLoadId[id] = acc;
        }

        if (p.IsHashIntervalTick(GenDate.TicksPerHour)
            && p.needs?.TryGetNeed(MilkCumDefOf.Sex) != null
            && MilkingApparelFlowUtility.GetSemenPoolPressureSquared(p) > 0.001f)
        {
            var props = new SexProps(p, null) { orgasms = 1 };
            SexUtility.SatisfyPersonal(props, 1);
        }
    }
}
