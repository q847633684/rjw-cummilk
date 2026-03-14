using RimWorld;
using Verse;
using MilkCum.Fluids.Cum.Leaking;

namespace MilkCum.UI;

/// <summary>濂惰〃鏍煎垪锛氬浣忥紙闃叉绮炬恫鑷劧娉勬紡锛夈€備粎浜哄舰鎴愪汉涓旀湁闃撮亾鏃舵樉绀恒€</summary>
public class PawnColumnWorker_MilkSealCum : PawnColumnWorker_Checkbox
{
    protected override bool HasCheckbox(Pawn pawn)
    {
        if (pawn?.DevelopmentalStage != DevelopmentalStage.Adult) return false;
        var comp = pawn.TryGetComp<Comp_SealCum>();
        return comp != null && comp.canSeal();
    }

    protected override bool GetValue(Pawn pawn)
    {
        return pawn.TryGetComp<Comp_SealCum>()?.IsSealed() ?? false;
    }

    protected override void SetValue(Pawn pawn, bool value, PawnTable table)
    {
        pawn.TryGetComp<Comp_SealCum>()?.SetSealed(value);
    }

    protected override string GetHeaderTip(PawnTable table)
    {
        return "EM.Milk_SealCumTip".Translate();
    }
}
