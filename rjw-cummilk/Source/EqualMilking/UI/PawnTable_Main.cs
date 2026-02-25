using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using EqualMilking.Helpers;
using System;

namespace EqualMilking;

public class MainTabWindow : MainTabWindow_PawnTable
{
    private string buttonText = Lang.All.CapitalizeFirst();
    protected override float ExtraBottomSpace => 53f; //default 53
    protected override float ExtraTopSpace => 40f; //default 0 //40 for button
    protected override PawnTableDef PawnTableDef => EMDefOf.Milk_PawnTable;
    protected override IEnumerable<Pawn> Pawns => pawns;
    private IEnumerable<Pawn> pawns = Find.CurrentMap.mapPawns.AllPawns.Where(p => p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony || p.IsSlaveOfColony || p.IsOnHoldingPlatform);
    //the commented out sections would only be needed if we had multiple tabs and a button to swap between them 
    //which we now do

    public override void DoWindowContents(Rect rect)
    {
        base.DoWindowContents(rect);
        if (Widgets.ButtonText(new Rect(rect.x + 5f, rect.y + 5f, Mathf.Min(rect.width, 260f), 32f), buttonText, true, true, true))
        {
            MakeMenu();
        }
    }

    public override void PostOpen()
    {
        base.PostOpen();
        Find.World.renderer.wantedMode = WorldRenderMode.None;
    }

    public void MakeMenu()
    {
        Find.WindowStack.Add(new FloatMenu(MakeOptions()));
    }

    public List<FloatMenuOption> MakeOptions()
    {
        List<FloatMenuOption> opts = new()
        {
            MakeOption(Lang.All, AllPawnsOption()),
            MakeOption(Lang.Human, HumanPawnsOption()),
            MakeOption(Lang.Colonist, ColonistPawnsOption()),
            MakeOption(Lang.Prisoner, PrisonerPawnsOption()),
            MakeOption(Lang.Slave, SlavePawnsOption()),
            MakeOption(Lang.Animal, AnimalPawnsOption()),
            MakeOption(Lang.Mechanoid, MechanoidPawnsOption())
        };
        if (ModsConfig.AnomalyActive)
        {
            opts.Add(MakeOption(Lang.Entities, EntityPawnsOption()));
        }
        return opts;
    }
    private FloatMenuOption MakeOption(string label, Func<IEnumerable<Pawn>> action)
    {
        label = label.CapitalizeFirst();
        return new FloatMenuOption(label, () =>
        {
            pawns = action();
            buttonText = label;
            Notify_ResolutionChanged();
        }, MenuOptionPriority.Default);
    }
    private Func<IEnumerable<Pawn>> AllPawnsOption() => () => Find.CurrentMap.mapPawns.AllPawns.Where(p => (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony || p.IsSlaveOfColony || p.IsOnHoldingPlatform) && p.CompEquallyMilkable()?.MilkSettings != null == true);
    private Func<IEnumerable<Pawn>> HumanPawnsOption() => () => Find.CurrentMap.mapPawns.AllPawns.Where(p => p.RaceProps.Humanlike && (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony || p.IsSlaveOfColony) && p.CompEquallyMilkable()?.MilkSettings != null == true);
    private Func<IEnumerable<Pawn>> ColonistPawnsOption() => () => Find.CurrentMap.mapPawns.AllPawns.Where(p => p.IsColonist && !p.IsPrisonerOfColony && !p.IsSlaveOfColony && p.CompEquallyMilkable()?.MilkSettings != null == true);
    private Func<IEnumerable<Pawn>> PrisonerPawnsOption() => () => Find.CurrentMap.mapPawns.AllPawns.Where(p => p.IsPrisonerOfColony && p.CompEquallyMilkable()?.MilkSettings != null == true);
    private Func<IEnumerable<Pawn>> SlavePawnsOption() => () => Find.CurrentMap.mapPawns.AllPawns.Where(p => p.IsSlaveOfColony && p.CompEquallyMilkable()?.MilkSettings != null == true);
    private Func<IEnumerable<Pawn>> AnimalPawnsOption() => () => Find.CurrentMap.mapPawns.AllPawns.Where(p => p.RaceProps.Animal && p.Faction == Faction.OfPlayer && p.CompEquallyMilkable()?.MilkSettings != null == true);
    private Func<IEnumerable<Pawn>> MechanoidPawnsOption() => () => Find.CurrentMap.mapPawns.AllPawns.Where(p => p.IsColonyMech && p.CompEquallyMilkable()?.MilkSettings != null == true);
    private Func<IEnumerable<Pawn>> EntityPawnsOption() => () => Find.CurrentMap.mapPawns.AllPawns.Where(p => p.IsOnHoldingPlatform && p.CompEquallyMilkable()?.MilkSettings != null == true);
}