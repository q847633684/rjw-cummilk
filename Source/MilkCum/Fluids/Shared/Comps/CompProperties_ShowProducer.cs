using System.Linq;
using MilkCum.Core;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Shared.Comps;

/// <summary>奶/精液等流体物品的产主标记 Comp（奶与精液共用）。见 CumpilationIntegration、CompEquallyMilkable.Milking。</summary>
public class CompProperties_ShowProducer : CompProperties
{
    public CompProperties_ShowProducer()
    {
        compClass = typeof(CompShowProducer);
    }
}

/// <summary>在物品上记录产主 (producer/producerKind)，用于 label、堆叠、检查面板、食用许可名单。奶制品与精液制品共用。</summary>
public class CompShowProducer : ThingComp
{
    public CompProperties_ShowProducer Props => (CompProperties_ShowProducer)props;
    public Pawn producer;
    public PawnKindDef producerKind;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref producer, "ProducerPawn");
        Scribe_Defs.Look(ref producerKind, "ProducerKind");
    }

    public override string TransformLabel(string label)
    {
        label = base.TransformLabel(label);
        // 勿在「未显示标签」时清空 producer/producerKind：否则每次绘制标签都会抹掉存档数据与食用校验依据。
        if (producerKind != null && MilkCumSettings.HasRaceTag(parent))
            label = Lang.Join(producerKind.race.label, label);
        if (producer != null && MilkCumSettings.HasPawnTag(parent))
            label = "SomeonesRoom".Translate().Replace("{PAWN_labelShort}", producer.LabelShort).Replace("{1}", label);
        return label;
    }

    public override bool AllowStackWith(Thing other)
    {
        var otherComp = other.TryGetComp<CompShowProducer>();
        if (otherComp == null) return false;
        return otherComp.producer == producer && otherComp.producerKind == producerKind;
    }

    /// <summary>物品悬浮/检查面板：显示产主与许可食用者名单（名单来自产主的 CompEquallyMilkable.allowedConsumers）。</summary>
    public override string CompInspectStringExtra()
    {
        if (producer == null) return null;
        string producerLine = "EM.ItemProducer".Translate(producer.LabelShort);
        var producerComp = producer.CompEquallyMilkable();
        string allowedLine;
        if (producerComp?.allowedConsumers == null || producerComp.allowedConsumers.Count == 0)
            allowedLine = "EM.ItemAllowedConsumersOwnerOnly".Translate();
        else
            allowedLine = "EM.ItemAllowedConsumers".Translate(string.Join(", ", producerComp.allowedConsumers.Where(p => p != null && !p.Destroyed).Select(p => p.LabelShort)));
        return producerLine + "\n" + allowedLine;
    }
}
