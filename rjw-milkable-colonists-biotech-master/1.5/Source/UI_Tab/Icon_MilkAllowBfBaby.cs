using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Milk.MainTab.Icon
{
    [StaticConstructorOnStartup]
    public class PawnColumnWorker_MilkAllowBfBaby : PawnColumnWorker_Icon
    {
        private readonly Texture2D comfortOn = ContentFinder<Texture2D>.Get("UI/Commands/ComfortPrisoner_on");
        private readonly Texture2D comfortOff = ContentFinder<Texture2D>.Get("UI/Commands/ComfortPrisoner_off");
        private readonly Texture2D notAvailableTex = ContentFinder<Texture2D>.Get("UI/Commands/cancel");
        protected override Texture2D GetIconFor(Pawn pawn)
        {
            return pawn.CanDesignateMilkAllowBfBaby() ? pawn.IsDesignatedMilkAllowBfBaby() ? comfortOn : comfortOff : notAvailableTex;
        }
        protected override string GetIconTip(Pawn pawn)
        {
            return "PawnColumnWorker_MilkAllowBfBaby".Translate();
            ;
        }
    }
}