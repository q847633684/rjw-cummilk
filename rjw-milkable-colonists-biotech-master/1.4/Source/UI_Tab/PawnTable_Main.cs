using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Milk.MainTab.DefModExtensions;
using rjw;
using rjw.MainTab.DefModExtensions;

namespace Milk.MainTab
{
    [StaticConstructorOnStartup]
    public class Milk_PawnTableList
    {
        public List<PawnTableDef> getdefs()
        {
            var defs = new List<PawnTableDef>();
            defs.AddRange(DefDatabase<PawnTableDef>.AllDefs.Where(x => x.HasModExtension<Milk_PawnTable>()));
            return defs;
        }
    }
    public class MainTabWindow : MainTabWindow_PawnTable
    {
        protected override float ExtraBottomSpace
        {
            get
            {
                return 53f; //default 53
            }
        }

        protected override float ExtraTopSpace
        {
            get
            {
                return 40f; //default 0 //40 for button
            }
        }

        protected override PawnTableDef PawnTableDef => pawnTableDef;

        protected override IEnumerable<Pawn> Pawns => pawns;

        public IEnumerable<Pawn> pawns = Find.CurrentMap.mapPawns.AllPawns.Where(p => p.RaceProps.Humanlike && p.IsColonist);

        //public IEnumerable<Pawn> pawns = Find.CurrentMap.mapPawns.AllPawns.Where(p => p.RaceProps.Humanlike && p.IsColonist && !xxx.is_slave(p));
        public PawnTableDef pawnTableDef = DefDatabase<PawnTableDef>.GetNamed("Milk_PawnTable_Colonists");



        //the commented out sections would only be needed if we had multiple tabs and a button to swap between them 
        //which we now do

        public override void DoWindowContents(Rect rect)
        {
            base.DoWindowContents(rect);
            if (Widgets.ButtonText(new Rect(rect.x + 5f, rect.y + 5f, Mathf.Min(rect.width, 260f), 32f), "MilkTableMain_Designators".Translate(), true, true, true))
            {
                MakeMenu();
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            Find.World.renderer.wantedMode = WorldRenderMode.None;
        }

        public static void Reloadtab()
        {
            var milktab = DefDatabase<MainButtonDef>.GetNamed("Milk_MainButton");
            Find.MainTabsRoot.ToggleTab(milktab, false);//off
            Find.MainTabsRoot.ToggleTab(milktab, false);//on
        }

        
        public void MakeMenu()
        {
            Find.WindowStack.Add(new FloatMenu(MakeOptions()));
        }

        /// <summary>
        /// switch pawnTable's
        /// patch this
        /// </summary>
        
        
        public List<FloatMenuOption> MakeOptions()
        {
            List<FloatMenuOption> opts = new List<FloatMenuOption>();
            PawnTableDef tabC = DefDatabase<PawnTableDef>.GetNamed("Milk_PawnTable_Colonists");
            PawnTableDef tabP = DefDatabase<PawnTableDef>.GetNamed("Milk_PawnTable_Property");

            opts.Add(new FloatMenuOption(tabC.GetModExtension<Milk_PawnTable>().label, () =>
            {
                pawnTableDef = tabC;
                pawns = Find.CurrentMap.mapPawns.AllPawns.Where(p => p.RaceProps.Humanlike && p.IsColonist && !xxx.is_slave(p));
                Notify_ResolutionChanged();
                Reloadtab();
            }, MenuOptionPriority.Default));

            opts.Add(new FloatMenuOption(tabP.GetModExtension<Milk_PawnTable>().label, () =>
            {
                pawnTableDef = tabP;
                pawns = Find.CurrentMap.mapPawns.AllPawns.Where(p => p.RaceProps.Humanlike && (p.IsColonist && xxx.is_slave(p) || p.IsPrisonerOfColony));
                Notify_ResolutionChanged();
                Reloadtab();
            }, MenuOptionPriority.Default));

            return opts;
        }
    }
}
