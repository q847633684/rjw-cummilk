using RimWorld;
using Verse;
using EqualMilking.Helpers;

namespace EqualMilking;

public class PawnColumnWorker_MilkAllowBreastFeeding : PawnColumnWorker_MilkProducer
{
    protected override string GetHeaderTip(PawnTable table)
    {
        return Lang.Breastfeed + "(" + Lang.Baby + ")" + "\n\n" + base.GetHeaderTip(table);
    }

    protected override bool GetValue(Pawn pawn) => pawn.AllowBreastFeeding();
    protected override void SetValue(Pawn pawn, bool value, PawnTable table) => pawn.SetAllowBreastFeeding(value);
    protected override bool IsDisabled(Pawn pawn)
    {
        return base.IsDisabled(pawn) || pawn.workSettings?.GetPriority(WorkTypeDefOf.Childcare) == 0;
    }
    protected override string GetTip(Pawn pawn)
    {
        if (pawn.workSettings?.Initialized != true //Shambler
            || pawn.workSettings?.GetPriority(WorkTypeDefOf.Childcare) == 0) //Disabled
        {
            string baseTip = base.GetTip(pawn).NullOrEmpty() ? "" : base.GetTip(pawn) + "\n";
            return baseTip + "NotAssignedToWorkType".Translate(WorkTypeDefOf.Childcare.labelShort);
        }
        return base.GetTip(pawn);
    }
    protected override bool HasCheckbox(Pawn pawn)
    {
        return base.HasCheckbox(pawn) && !pawn.IsOnHoldingPlatform;
    }
}
