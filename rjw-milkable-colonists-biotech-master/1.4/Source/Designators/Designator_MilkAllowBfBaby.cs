using Verse;
using RimWorld;

namespace Milk
{
    public static class Designator_MilkAllowBaby
    {
        public static bool UpdateCanDesignateMilkAllowBfBaby(this Pawn pawn)
        {
            if (!ModsConfig.BiotechActive)
            {
                return pawn.GetMilkPawnData().CanDesignateMilkAllowBfBaby = false;
            }
            if (!pawn.Dead)
            {
                //to do: return false if the pawn is a baby
                //probably more things too.

                return pawn.GetMilkPawnData().CanDesignateMilkAllowBfBaby = true;
            }

            return pawn.GetMilkPawnData().CanDesignateMilkAllowBfBaby = false;
        }
        public static bool CanDesignateMilkAllowBfBaby(this Pawn pawn)
        {
            return pawn.GetMilkPawnData().CanDesignateMilkAllowBfBaby;
        }
        public static void ToggleMilkAllowBfBaby(this Pawn pawn)
        {
            pawn.UpdateCanDesignateMilkAllowBfBaby();
            if (pawn.CanDesignateMilkAllowBfBaby())
            {
                if (!pawn.IsDesignatedMilkAllowBfBaby())
                    DesignateMilkAllowBfBaby(pawn);
                else
                    UnDesignateMilkAllowBfBaby(pawn);
            }
        }
        public static bool IsDesignatedMilkAllowBfBaby(this Pawn pawn)
        {
            if (pawn.GetMilkPawnData().MilkAllowBfBaby)
            {
                if (pawn.Dead)
                    pawn.UnDesignateMilkAllowBfBaby();
            }
            if (!ModsConfig.BiotechActive)
            {
                pawn.UnDesignateMilkAllowBfBaby();
            }
            return pawn.GetMilkPawnData().MilkAllowBfBaby;
        }
        //[SyncMethod]
        public static void DesignateMilkAllowBfBaby(this Pawn pawn)
        {
            if (ModsConfig.BiotechActive)
            {
                DesignatorsData.milkAllowBfBaby.AddDistinct(pawn);
                pawn.GetMilkPawnData().MilkAllowBfBaby = true;
            }
            else
            {
                DesignatorsData.milkAllowBfBaby.Remove(pawn);
                pawn.GetMilkPawnData().MilkAllowBfBaby = false;
            }

        }
        //[SyncMethod]
        public static void UnDesignateMilkAllowBfBaby(this Pawn pawn)
        {
            DesignatorsData.milkAllowBfBaby.Remove(pawn);
            pawn.GetMilkPawnData().MilkAllowBfBaby = false;
        }
    }
}
