using Verse;
using System.Diagnostics;
using RimWorld;
using rjw;

namespace Milk
{
    public static class PawnDesignations_Utility
    {
        public static bool UpdatePermissions(this Pawn pawn)
        {
            pawn.UpdateCanDesignateMilkAllowBfAdult();
            pawn.UpdateCanDesignateMilkAllowBfBaby();
            pawn.UpdateCanDesignateMilkAllowMachine();
            pawn.UpdateCanDesignateMilkAllowManual();
            pawn.UpdateCanDesignateMilkAllowSelf();
            pawn.UpdateCanDesignateMilkWillBfAdult();
            pawn.UpdateCanDesignateMilkWillManual();
            pawn.UpdateCanDesignateMilkShouldProduce();

            return true;
        }
    }
}
