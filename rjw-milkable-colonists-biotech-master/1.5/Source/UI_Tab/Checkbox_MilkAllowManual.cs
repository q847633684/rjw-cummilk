using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace Milk.MainTab.Checkbox
{
    [StaticConstructorOnStartup]
    public class PawnColumnWorker_MilkAllowManual : PawnColumnWorker_Checkbox
    {
        public static readonly Texture2D CheckboxOnTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_on");
        public static readonly Texture2D CheckboxOffTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_off");
        public static readonly Texture2D CheckboxDisabledTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_Refuse");
        protected override bool HasCheckbox(Pawn pawn)
        {
            return pawn.CanDesignateMilkAllowManual();
        }
        protected bool GetDisabled(Pawn pawn)
        {
            return !pawn.CanDesignateMilkAllowManual();
        }

        protected override bool GetValue(Pawn pawn)
        {
            //return pawn.IsDesignatedMilkAllowBfAdult() && xxx.is_animal(pawn);
            return pawn.IsDesignatedMilkAllowManual();
        }

        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            if (value == this.GetValue(pawn)) return;
            pawn.ToggleMilkAllowManual();
        }
    }
}