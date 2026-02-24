using Verse;
using RimWorld;

namespace Milk
{
    public static class Designator_MilkAllowSelf
    {
        public static bool UpdateCanDesignateMilkAllowSelf(this Pawn pawn)
        {
            if (!pawn.Dead)
            {
                //to do: return false if the pawn is a baby
                //probably more things too.

                return pawn.GetMilkPawnData().CanDesignateMilkAllowSelf = true;
            }

            return pawn.GetMilkPawnData().CanDesignateMilkAllowSelf = false;
        }
        public static bool CanDesignateMilkAllowSelf(this Pawn pawn)
        {
            return pawn.GetMilkPawnData().CanDesignateMilkAllowSelf;
        }
        public static void ToggleMilkAllowSelf(this Pawn pawn)
        {
            pawn.UpdateCanDesignateMilkAllowSelf();
            if (pawn.CanDesignateMilkAllowSelf())
            {
                if (!pawn.IsDesignatedMilkAllowSelf())
                    DesignateMilkAllowSelf(pawn);
                else
                    UnDesignateMilkAllowSelf(pawn);
            }
        }
        public static bool IsDesignatedMilkAllowSelf(this Pawn pawn)
        {
            if (pawn.GetMilkPawnData().MilkAllowSelf)
            {
                if (pawn.Dead)
                    pawn.UnDesignateMilkAllowSelf();
            }
            return pawn.GetMilkPawnData().MilkAllowSelf;
        }
        //[SyncMethod]
        public static void DesignateMilkAllowSelf(this Pawn pawn)
        {
            DesignatorsData.milkAllowSelf.AddDistinct(pawn);
            pawn.GetMilkPawnData().MilkAllowSelf = true;
        }
        //[SyncMethod]
        public static void UnDesignateMilkAllowSelf(this Pawn pawn)
        {
            DesignatorsData.milkAllowSelf.Remove(pawn);
            pawn.GetMilkPawnData().MilkAllowSelf = false;
        }
    }
}
