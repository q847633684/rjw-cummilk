using Verse;
using RimWorld;

namespace Milk
{
    public static class Designator_MilkAllowMachine
    {
        public static bool UpdateCanDesignateMilkAllowMachine(this Pawn pawn)
        {
            if (!pawn.Dead)
            {
                //to do: return false if the pawn is a baby
                //probably more things too.

                return pawn.GetMilkPawnData().CanDesignateMilkAllowMachine = true;
            }

            return pawn.GetMilkPawnData().CanDesignateMilkAllowMachine = false;
        }
        public static bool CanDesignateMilkAllowMachine(this Pawn pawn)
        {
            return pawn.GetMilkPawnData().CanDesignateMilkAllowMachine;
        }
        public static void ToggleMilkAllowMachine(this Pawn pawn)
        {
            pawn.UpdateCanDesignateMilkAllowMachine();
            if (pawn.CanDesignateMilkAllowMachine())
            {
                if (!pawn.IsDesignatedMilkAllowMachine())
                    DesignateMilkAllowMachine(pawn);
                else
                    UnDesignateMilkAllowMachine(pawn);
            }
        }
        public static bool IsDesignatedMilkAllowMachine(this Pawn pawn)
        {
            if (pawn.GetMilkPawnData().MilkAllowMachine)
            {
                if (pawn.Dead)
                    pawn.UnDesignateMilkAllowMachine();
            }
            return pawn.GetMilkPawnData().MilkAllowMachine;
        }
        //[SyncMethod]
        public static void DesignateMilkAllowMachine(this Pawn pawn)
        {
            DesignatorsData.milkAllowMachine.AddDistinct(pawn);
            pawn.GetMilkPawnData().MilkAllowMachine = true;
        }
        //[SyncMethod]
        public static void UnDesignateMilkAllowMachine(this Pawn pawn)
        {
            DesignatorsData.milkAllowMachine.Remove(pawn);
            pawn.GetMilkPawnData().MilkAllowMachine = false;
        }
    }
}
