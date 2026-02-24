using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace Milk.MainTab.Checkbox
{
    [StaticConstructorOnStartup]
    public class PawnColumnWorker_MilkShouldProduce : PawnColumnWorker_Checkbox
    {
        public static readonly Texture2D CheckboxOnTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_on");
        public static readonly Texture2D CheckboxOffTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_off");
        public static readonly Texture2D CheckboxDisabledTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_Refuse");
        protected override bool HasCheckbox(Pawn pawn)
        {
            return pawn.CanDesignateMilkShouldProduce();
        }
        protected bool GetDisabled(Pawn pawn)
        {
            return !pawn.CanDesignateMilkShouldProduce();
        }

        protected override bool GetValue(Pawn pawn)
        {
            //return pawn.IsDesignatedMilkAllowBfAdult() && xxx.is_animal(pawn);
            return pawn.IsDesignatedMilkShouldProduce();
        }

        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            if (value == this.GetValue(pawn)) return;
            pawn.ToggleMilkShouldProduce();
        }
    }
}