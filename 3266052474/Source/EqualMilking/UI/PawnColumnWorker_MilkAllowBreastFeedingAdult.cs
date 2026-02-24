using RimWorld;
using Verse;
using EqualMilking.Helpers;

namespace EqualMilking;
public class PawnColumnWorker_MilkAllowBreastFeedingAdult : PawnColumnWorker_MilkProducer
{
    protected override string GetHeaderTip(PawnTable table)
    {
        return Lang.Breastfeed + "(" + Lang.Adult + ")" + "\n\n" + base.GetHeaderTip(table);
    }

    protected override bool GetValue(Pawn pawn) => pawn.AllowBreastFeedingAdult();
    protected override void SetValue(Pawn pawn, bool value, PawnTable table) => pawn.SetAllowBreastFeedingAdult(value);
    protected override bool HasCheckbox(Pawn pawn)
    {
        return base.HasCheckbox(pawn) && !pawn.IsOnHoldingPlatform;
    }
}
