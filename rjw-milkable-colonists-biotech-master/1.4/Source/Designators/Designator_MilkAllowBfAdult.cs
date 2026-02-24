using Verse;
using RimWorld;

namespace Milk
{
    public static class Designator_MilkAllowAdult
    {
        public static bool UpdateCanDesignateMilkAllowBfAdult(this Pawn pawn)
        {
            if (!pawn.Dead)
            {
                //to do: return false if the pawn is a baby
                //probably more things too.

                return pawn.GetMilkPawnData().CanDesignateMilkAllowBfAdult = true;
            }
            
            return pawn.GetMilkPawnData().CanDesignateMilkAllowBfAdult = false;
        }
        public static bool CanDesignateMilkAllowBfAdult(this Pawn pawn)
        {
            return pawn.GetMilkPawnData().CanDesignateMilkAllowBfAdult;
        }
        public static void ToggleMilkAllowBfAdult(this Pawn pawn)
        {
            pawn.UpdateCanDesignateMilkAllowBfAdult();
            if (pawn.CanDesignateMilkAllowBfAdult())
            {
                if (!pawn.IsDesignatedMilkAllowBfAdult())
                    DesignateMilkAllowBfAdult(pawn);
                else
                    UnDesignateMilkAllowBfAdult(pawn);
            }
        }
        public static bool IsDesignatedMilkAllowBfAdult(this Pawn pawn)
        {
            if (pawn.GetMilkPawnData().MilkAllowBfAdult)
            {
                if (pawn.Dead)
                    pawn.UnDesignateMilkAllowBfAdult();
            }
            return pawn.GetMilkPawnData().MilkAllowBfAdult;
        }
        //[SyncMethod]
        public static void DesignateMilkAllowBfAdult(this Pawn pawn)
        {
            DesignatorsData.milkAllowBfAdult.AddDistinct(pawn);
            pawn.GetMilkPawnData().MilkAllowBfAdult = true;
        }
        //[SyncMethod]
        public static void UnDesignateMilkAllowBfAdult(this Pawn pawn)
        {
            DesignatorsData.milkAllowBfAdult.Remove(pawn);
            pawn.GetMilkPawnData().MilkAllowBfAdult = false;
        }
    }
}
