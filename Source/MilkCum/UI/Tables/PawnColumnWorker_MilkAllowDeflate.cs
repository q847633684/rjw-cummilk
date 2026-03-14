using RimWorld;
using Verse;
using MilkCum.Fluids.Cum.Cumflation;
using MilkCum.Fluids.Cum.Leaking;

namespace MilkCum.UI;

/// <summary>濂惰〃鏍煎垪锛氭槸鍚﹀厑璁告硠绮撅紙鍘诲仛娉勫埌妗?娓呮磥/闅忓湴鐨?Job锛夈€</summary>
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
