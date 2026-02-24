using RimWorld;
using Verse;
using EqualMilking.Helpers;

namespace EqualMilking;
public class PawnColumnWorker_MilkAllowManual : PawnColumnWorker_MilkProducer
{
    protected override string GetHeaderTip(PawnTable table)
    {
        return Lang.Milking + "\n\n" + base.GetHeaderTip(table);
    }
    protected override bool GetValue(Pawn pawn) => pawn.AllowMilking();
    protected override void SetValue(Pawn pawn, bool value, PawnTable table) => pawn.SetAllowMilking(value);
}
