using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using EqualMilking.Helpers;
using EqualMilking.Data;
using static EqualMilking.Helpers.Constants;
using System;
using static EqualMilking.Helpers.Categories;

namespace EqualMilking.UI;

public class Widget_DefaultSetting
{
    private readonly MilkSettings colonistSetting;
    private readonly MilkSettings slaveSetting;
    private readonly MilkSettings prisonerSetting;
    private readonly MilkSettings animalSetting;
    private readonly MilkSettings mechSetting;
    private readonly MilkSettings entitySetting;
    private static int tabIndex = (int)PawnCategory.Colonist;
    private static List<TabRecord> tabs;
    public Widget_DefaultSetting(MilkSettings colonistSetting,
                                 MilkSettings slaveSetting,
                                 MilkSettings prisonerSetting,
                                 MilkSettings animalSetting,
                                 MilkSettings mechSetting,
                                 MilkSettings entitySetting)
    {
        this.colonistSetting = colonistSetting;
        this.slaveSetting = slaveSetting;
        this.prisonerSetting = prisonerSetting;
        this.animalSetting = animalSetting;
        this.mechSetting = mechSetting;
        this.entitySetting = entitySetting;
    }
    public void Draw(Rect inRect)
    {
        tabs ??= new List<TabRecord>();
        tabs.Clear();
        MilkSettings setting = null;
        foreach (PawnCategory category in Enum.GetValues(typeof(PawnCategory)).Cast<PawnCategory>().Where(c => c != PawnCategory.None))
        {
            bool selected = tabIndex == (int)category;
            tabs.Add(new TabRecord(category.Label(), () => tabIndex = (int)category, selected));
            if (selected)
            {
                setting = category switch
                {
                    PawnCategory.Colonist => colonistSetting,
                    PawnCategory.Slave => slaveSetting,
                    PawnCategory.Prisoner => prisonerSetting,
                    PawnCategory.Animal => animalSetting,
                    PawnCategory.Mechanoid => mechSetting,
                    PawnCategory.Entity => entitySetting,
                    _ => null
                };
            }
        }
        TabDrawer.DrawTabs(inRect.ContractedBy(UNIT_SIZE), tabs);
        if (setting == null) { return; }
        Rect contentRect = inRect.ContractedBy(UNIT_SIZE * 2f);
        Listing_Standard listing = new();
        listing.Begin(contentRect);
        listing.CheckboxLabeled(Lang.Milking + " (" + Lang.Self + ")", ref setting.allowMilkingSelf);
        listing.CheckboxLabeled("EM.DefaultSetting_CanBeFed".Translate(), ref setting.canBeFed);
        GUI.color = Color.gray;
        listing.Label("EM.DefaultSetting_CanBeFedTip".Translate(), -1f);
        GUI.color = Color.white;
        listing.End();
    }
}
