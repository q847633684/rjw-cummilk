using RimWorld;
using Verse;
using Cumpilation.Leaking;

namespace MilkCum.UI;

/// <summary>奶表格列：塞住（防止精液自然泄漏）。仅人形成人且有阴道时显示。</summary>
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
