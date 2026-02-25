using RimWorld;
using Verse;
using EqualMilking.Helpers;

namespace EqualMilking;
public class PawnColumnWorker_MilkAllowSelf : PawnColumnWorker_MilkProducer
{
    protected override string GetHeaderTip(PawnTable table)
    {
        return Lang.Milking + "(" + Lang.Self + ")" + "\n\n" + base.GetHeaderTip(table);
    }
    protected override bool GetValue(Pawn pawn) => pawn.AllowMilkingSelf();
    protected override void SetValue(Pawn pawn, bool value, PawnTable table) => pawn.SetAllowMilkingSelf(value);
}