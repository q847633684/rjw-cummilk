using System.Linq;
using MilkCum.Core;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Shared.Comps;

/// <summary>濂?绮炬恫绛夋祦浣撲骇鐗╃殑浜т富鏍囪 Comp锛堝ザ涓庣簿娑插叡鐢級銆傝 CumpilationIntegration銆丆ompEquallyMilkable.Milking銆</summary>
public class CompProperties_ShowProducer : CompProperties
{
    public CompProperties_ShowProducer()
    {
        compClass = typeof(CompShowProducer);
    }
}

/// <summary>鐗╁搧涓婅褰曚骇涓?producer/producerKind)锛岀敤浜?label銆佸爢鍙犮€佹鏌ラ潰鏉裤€侀鐢ㄥ厑璁稿悕鍗曘€傚ザ鍒跺搧涓庣簿娑插埗鍝佸叡鐢ㄣ€</summary>
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
        if (producerKind != null && MilkCumSettings.HasRaceTag(parent))
        {
            label = Lang.Join(producerKind.race.label, label);
        }
        else { producerKind = null; }
        if (producer != null && MilkCumSettings.HasPawnTag(parent))
        {
            label = "SomeonesRoom".Translate().Replace("{PAWN_labelShort}", producer.LabelShort).Replace("{1}", label);
        }
        else { producer = null; }
        return label;
    }

    public override bool AllowStackWith(Thing other)
    {
        var otherComp = other.TryGetComp<CompShowProducer>();
        if (otherComp == null) return false;
        return otherComp.producer == producer && otherComp.producerKind == producerKind;
    }

    /// <summary>鐗╁搧鎮诞/妫€鏌ラ潰鏉匡細鏄剧ず浜т富涓庡厑璁镐娇鐢ㄥ悕鍗曪紙濂朵骇涓荤敤 CompEquallyMilkable.allowedConsumers锛涚簿娑茬瓑鏃?Comp 鏃朵粎鏄剧ず浜т富锛夈€</summary>
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
