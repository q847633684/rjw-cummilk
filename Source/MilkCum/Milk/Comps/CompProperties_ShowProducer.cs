using MilkCum.Core;
using MilkCum.Milk.Helpers;
using RimWorld;
using Verse;

namespace MilkCum.Milk.Comps;
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
}
