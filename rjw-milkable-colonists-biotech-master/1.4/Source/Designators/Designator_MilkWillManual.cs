using Verse;
using RimWorld;
using Verse.AI;

namespace Milk
{
    public static class Designator_MilkWillManual
    {
        public static bool UpdateCanDesignateMilkWillManual(this Pawn pawn)
        {
            if (!pawn.Dead)
            {
                //to do: return false if the pawn is a baby
                //probably more things too.

                return pawn.GetMilkPawnData().CanDesignateMilkWillManual = true;
            }

            return pawn.GetMilkPawnData().CanDesignateMilkWillManual = false;
        }
        public static bool CanDesignateMilkWillManual(this Pawn pawn)
        {
            return pawn.GetMilkPawnData().CanDesignateMilkWillManual;
        }
        public static void ToggleMilkWillManual(this Pawn pawn)
        {
            pawn.UpdateCanDesignateMilkWillManual();
            if (pawn.CanDesignateMilkWillManual())
            {
                if (!pawn.IsDesignatedMilkWillManual())
                    DesignateMilkWillManual(pawn);
                else
                    UnDesignateMilkWillManual(pawn);
            }
        }
        public static bool IsDesignatedMilkWillManual(this Pawn pawn)
        {
            if (pawn.GetMilkPawnData().MilkWillManual)
            {
                if (pawn.Dead)
                    pawn.UnDesignateMilkWillManual();

                if (!pawn.IsColonist)
                    pawn.UnDesignateMilkWillManual();

            }
            return pawn.GetMilkPawnData().MilkWillManual;
        }
        //[SyncMethod]
        public static void DesignateMilkWillManual(this Pawn pawn)
        {
            DesignatorsData.milkWillManual.AddDistinct(pawn);
            pawn.GetMilkPawnData().MilkWillManual = true;
        }
        //[SyncMethod]
        public static void UnDesignateMilkWillManual(this Pawn pawn)
        {
            DesignatorsData.milkWillManual.Remove(pawn);
            pawn.GetMilkPawnData().MilkWillManual = false;
        }
    }
}
