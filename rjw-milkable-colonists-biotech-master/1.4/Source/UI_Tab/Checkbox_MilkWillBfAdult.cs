using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace Milk.MainTab.Checkbox
{
    [StaticConstructorOnStartup]
    public class PawnColumnWorker_MilkWillBfAdult : PawnColumnWorker_Checkbox
    {
        public static readonly Texture2D CheckboxOnTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_on");
        public static readonly Texture2D CheckboxOffTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_off");
        public static readonly Texture2D CheckboxDisabledTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_Refuse");
        protected override bool HasCheckbox(Pawn pawn)
        {
            return pawn.CanDesignateMilkWillBfAdult();
        }
        protected bool GetDisabled(Pawn pawn)
        {
            return !pawn.CanDesignateMilkWillBfAdult();
        }

        protected override bool GetValue(Pawn pawn)
        {
            //return pawn.IsDesignatedMilkAllowBfAdult() && xxx.is_animal(pawn);
            return pawn.IsDesignatedMilkWillBfAdult();
        }

        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            if (value == this.GetValue(pawn)) return;
            pawn.ToggleMilkWillBfAdult();
        }
    }
}