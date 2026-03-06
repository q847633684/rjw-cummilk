using MilkCum.Core;
using RimWorld;
using Verse;
using System.Linq;

namespace MilkCum.Fluids.Lactation.Comps;
public class CompProperties_ShowProducer : CompProperties
{
    public CompProperties_ShowProducer()
    {
        this.compClass = typeof(CompShowProducer);
    }
}
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
        if (producerKind != null && EqualMilkingSettings.HasRaceTag(this.parent))
        {
            label = Lang.Join(producerKind.race.label, label);
        }
        else { this.producerKind = null; }
        if (producer != null && EqualMilkingSettings.HasPawnTag(this.parent))
        {
            label = "SomeonesRoom".Translate().Replace("{PAWN_labelShort}", producer.LabelShort).Replace("{1}", label);
        }
        else { this.producer = null; }
        return label;
    }
    public override bool AllowStackWith(Thing other)
    {
        CompShowProducer otherComp = other.TryGetComp<CompShowProducer>();
        if (otherComp == null)
        {
            return false;
        }
        else
        {
            return otherComp.producer == producer && otherComp.producerKind == producerKind;
        }
    }

    /// <summary>物品悬浮/检查面板：显示产主与允许使用名单（只读现有 Comp 数据），减少重复点进设置</summary>
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
