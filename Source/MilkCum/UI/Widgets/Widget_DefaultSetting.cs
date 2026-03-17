using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using static MilkCum.Core.Constants.Constants;
using System;
using static MilkCum.Fluids.Lactation.Helpers.Categories;

namespace MilkCum.UI;

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
        // Tab 行占固定高度，内容区从下方起算，避免 contentRect 高度为负时只显示标签不显示内容
        const float TabRowHeight = 36f;
        Rect tabRect = new Rect(inRect.x + UNIT_SIZE, inRect.y, inRect.width - UNIT_SIZE * 2f, TabRowHeight);
        TabDrawer.DrawTabs(tabRect, tabs);
        if (setting == null) { return; }
        float contentHeight = Mathf.Max(0f, inRect.height - TabRowHeight - UNIT_SIZE);
        if (contentHeight <= 0f) { return; }
        Rect contentRect = new Rect(inRect.x + UNIT_SIZE, inRect.y + TabRowHeight, inRect.width - UNIT_SIZE * 2f, contentHeight);
        Listing_Standard listing = new();
        listing.Begin(contentRect);
        GUI.color = Color.gray;
        listing.Label("EM.SectionDesc_DefaultByRole".Translate());
        GUI.color = Color.white;
        listing.Gap(4f);
        listing.CheckboxLabeled("EM.DefaultSetting_CanBeFed".Translate(), ref setting.canBeFed);
        GUI.color = Color.gray;
        listing.Label("EM.DefaultSetting_CanBeFedTip".Translate(), -1f);
        GUI.color = Color.white;
        listing.End();
    }
}
