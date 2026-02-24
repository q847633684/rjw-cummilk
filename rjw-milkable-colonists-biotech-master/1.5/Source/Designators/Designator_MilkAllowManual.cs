using Verse;
using RimWorld;

namespace Milk
{
    public static class Designator_MilkAllowManual
    {
        public static bool UpdateCanDesignateMilkAllowManual(this Pawn pawn)
        {
            if (!pawn.Dead)
            {
                //to do: return false if the pawn is a baby
                //probably more things too.

                return pawn.GetMilkPawnData().CanDesignateMilkAllowManual = true;
            }

            return pawn.GetMilkPawnData().CanDesignateMilkAllowManual = false;
        }
        public static bool CanDesignateMilkAllowManual(this Pawn pawn)
        {
            return pawn.GetMilkPawnData().CanDesignateMilkAllowManual;
        }
        public static void ToggleMilkAllowManual(this Pawn pawn)
        {
            pawn.UpdateCanDesignateMilkAllowManual();
            if (pawn.CanDesignateMilkAllowManual())
            {
                if (!pawn.IsDesignatedMilkAllowManual())
                    DesignateMilkAllowManual(pawn);
                else
                    UnDesignateMilkAllowManual(pawn);
            }
        }
        public static bool IsDesignatedMilkAllowManual(this Pawn pawn)
        {
            if (pawn.GetMilkPawnData().MilkAllowManual)
            {
                if (pawn.Dead)
                    pawn.UnDesignateMilkAllowManual();
            }
            return pawn.GetMilkPawnData().MilkAllowManual;
        }
        //[SyncMethod]
        public static void DesignateMilkAllowManual(this Pawn pawn)
        {
            DesignatorsData.milkAllowManual.AddDistinct(pawn);
            pawn.GetMilkPawnData().MilkAllowManual = true;
        }
        //[SyncMethod]
        public static void UnDesignateMilkAllowManual(this Pawn pawn)
        {
            DesignatorsData.milkAllowManual.Remove(pawn);
            pawn.GetMilkPawnData().MilkAllowManual = false;
        }
    }
}
