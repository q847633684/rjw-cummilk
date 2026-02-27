using RimWorld;
using Verse;
using Cumpilation.Cumflation;
using Cumpilation.Leaking;

namespace MilkCum.UI;

/// <summary>奶表格列：是否允许泄精（去做泄到桶/清洁/随地的 Job）。</summary>
public class PawnColumnWorker_MilkAllowDeflate : PawnColumnWorker_Checkbox
{
    protected override bool HasCheckbox(Pawn pawn)
    {
        if (pawn?.DevelopmentalStage != DevelopmentalStage.Adult) return false;
        var comp = pawn.TryGetComp<Comp_SealCum>();
        return comp != null && comp.PlayerControlled && CumflationUtility.CanBeCumflated(pawn);
    }

    protected override bool GetValue(Pawn pawn)
    {
        return pawn.TryGetComp<Comp_SealCum>()?.CanDeflate() ?? true;
    }

    protected override void SetValue(Pawn pawn, bool value, PawnTable table)
    {
        pawn.TryGetComp<Comp_SealCum>()?.SetCanDeflate(value);
    }

    protected override string GetHeaderTip(PawnTable table)
    {
        return "EM.Milk_AllowDeflateTip".Translate();
    }
}
