using Verse;
using RimWorld;

namespace Milk
{
    public static class Designator_MilkWillBfAdult
    {
        public static bool UpdateCanDesignateMilkWillBfAdult(this Pawn pawn)
        {
            if (!pawn.Dead)
            {
                //to do: return false if the pawn is a baby
                //probably more things too.

                return pawn.GetMilkPawnData().CanDesignateMilkWillBfAdult = true;
            }

            return pawn.GetMilkPawnData().CanDesignateMilkWillBfAdult = false;
        }
        public static bool CanDesignateMilkWillBfAdult(this Pawn pawn)
        {
            return pawn.GetMilkPawnData().CanDesignateMilkWillBfAdult;
        }
        public static void ToggleMilkWillBfAdult(this Pawn pawn)
        {
            pawn.UpdateCanDesignateMilkWillBfAdult();
            if (pawn.CanDesignateMilkWillBfAdult())
            {
                if (!pawn.IsDesignatedMilkWillBfAdult())
                    DesignateMilkWillBfAdult(pawn);
                else
                    UnDesignateMilkWillBfAdult(pawn);
            }
        }
        public static bool IsDesignatedMilkWillBfAdult(this Pawn pawn)
        {
            if (pawn.GetMilkPawnData().MilkWillBfAdult)
            {
                if (pawn.Dead)
                    pawn.UnDesignateMilkWillBfAdult();
                if (!pawn.IsColonist)
                    pawn.UnDesignateMilkWillBfAdult();

            }
            return pawn.GetMilkPawnData().MilkWillBfAdult;
        }
        //[SyncMethod]
        public static void DesignateMilkWillBfAdult(this Pawn pawn)
        {
            DesignatorsData.milkWillBfAdult.AddDistinct(pawn);
            pawn.GetMilkPawnData().MilkWillBfAdult = true;
        }
        //[SyncMethod]
        public static void UnDesignateMilkWillBfAdult(this Pawn pawn)
        {
            DesignatorsData.milkWillBfAdult.Remove(pawn);
            pawn.GetMilkPawnData().MilkWillBfAdult = false;
        }
    }
}
