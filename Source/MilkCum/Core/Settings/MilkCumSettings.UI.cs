using System.Collections.Generic;
using MilkCum.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>`MilkCumSettings` 的设置窗口状态与 UI 渲染分块。</summary>
internal partial class MilkCumSettings
{
	private int mainTabIndex = 0;
	private int subTabIndex = 0;
	private static readonly float unitSize = 32f;

	private static Widget_MilkableTable milkableTable;
	private static Widget_MilkTagsTable milkTagsTable;
	private static Widget_AdvancedSettings advancedSettings;
	private static Widget_BreastfeedSettings breastfeedSettings;
	private static Widget_GeneSetting geneSetting;
	private static Widget_DefaultSetting defaultSettingWidget;
	private static Widget_CumpilationSettings cumpilationSettings;
	private static Widget_RaceOverrides raceOverridesWidget;
	private int _prevMainTabForPawnCache = int.MinValue;

	public void DoWindowContents(Rect inRect)
	{
		inRect.yMin += unitSize;

		bool needPawnCache = pawnDefs == null
			|| (mainTabIndex == (int)MainTabIndex.Data && _prevMainTabForPawnCache != (int)MainTabIndex.Data);
		if (needPawnCache)
		{
			pawnDefs = GetMilkablePawns();
			defaultMilkProducts = GetDefaultMilkProducts();
		}
		_prevMainTabForPawnCache = mainTabIndex;

		List<TabRecord> mainTabs = new()
		{
			new("EM.Tab.Lactation".Translate(), () => { mainTabIndex = (int)MainTabIndex.Lactation; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Lactation),
			new("EM.Tab.Semen".Translate(), () => { mainTabIndex = (int)MainTabIndex.Semen; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Semen),
			new("EM.Tab.Nectar".Translate(), () => { mainTabIndex = (int)MainTabIndex.Nectar; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Nectar),
			new("EM.Tab.Permissions".Translate(), () => { mainTabIndex = (int)MainTabIndex.Permissions; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Permissions),
			new("EM.Tab.Data".Translate(), () => { mainTabIndex = (int)MainTabIndex.Data; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Data),
			new("EM.Tab.Integrations".Translate(), () => { mainTabIndex = (int)MainTabIndex.Integrations; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Integrations)
		};
		if (Prefs.DevMode)
			mainTabs.Add(new TabRecord("EM.Tab.Debug".Translate(), () => { mainTabIndex = (int)MainTabIndex.Debug; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Debug));
		if (mainTabIndex >= mainTabs.Count) mainTabIndex = 0;
		TabDrawer.DrawTabs(inRect, mainTabs);
		inRect.yMin += unitSize;

		List<TabRecord> subTabs = GetSubTabs();
		if (subTabIndex >= subTabs.Count) subTabIndex = 0;
		if (subTabs.Count > 0)
		{
			TabDrawer.DrawTabs(inRect, subTabs);
			inRect.yMin += unitSize;
		}

		Widgets.DrawMenuSection(inRect);
		Rect contentRect = inRect.ContractedBy(unitSize / 2);

		bool useAdvancedSettings =
			(mainTabIndex == (int)MainTabIndex.Lactation && subTabIndex > 0)
			|| (mainTabIndex == (int)MainTabIndex.Semen && subTabIndex == 1)
			|| (mainTabIndex == (int)MainTabIndex.Permissions && subTabIndex == 0)
			|| mainTabIndex == (int)MainTabIndex.Integrations
			|| mainTabIndex == (int)MainTabIndex.Debug;
		if (useAdvancedSettings)
			advancedSettings ??= new Widget_AdvancedSettings();

		switch (mainTabIndex)
		{
			case (int)MainTabIndex.Lactation:
				breastfeedSettings ??= new Widget_BreastfeedSettings(humanlikeBreastfeed, animalBreastfeed, mechanoidBreastfeed);
				if (subTabIndex == 0)
					breastfeedSettings.DrawBreastfeedSystemFull(contentRect);
				else
					advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Lactation, subTabIndex);
				break;
			case (int)MainTabIndex.Semen:
				cumpilationSettings ??= new Widget_CumpilationSettings();
				if (subTabIndex == 0)
					cumpilationSettings.Draw(contentRect);
				else
					advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Semen, subTabIndex);
				break;
			case (int)MainTabIndex.Nectar:
				DrawNectarPlaceholder(contentRect);
				break;
			case (int)MainTabIndex.Permissions:
				defaultSettingWidget ??= new Widget_DefaultSetting(colonistSetting, slaveSetting, prisonerSetting, animalSetting, mechSetting, entitySetting);
				if (subTabIndex == 0)
					advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Permissions, 0);
				else
					defaultSettingWidget.Draw(contentRect);
				break;
			case (int)MainTabIndex.Integrations:
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Integrations, subTabIndex);
				break;
			case (int)MainTabIndex.Data:
				milkableTable ??= new Widget_MilkableTable(namesToProducts);
				milkTagsTable ??= new Widget_MilkTagsTable(namesToProducts, productsToTags);
				raceOverridesWidget ??= new Widget_RaceOverrides();
				geneSetting ??= new Widget_GeneSetting(genes);
				if (subTabIndex == 0) milkableTable.Draw(contentRect);
				else if (subTabIndex == 1) milkTagsTable.Draw(contentRect);
				else if (subTabIndex == 2) raceOverridesWidget.Draw(contentRect);
				else geneSetting.Draw(contentRect);
				break;
			case (int)MainTabIndex.Debug:
				advancedSettings.DrawDevModeSection(contentRect);
				break;
		}
	}

	private static void DrawNectarPlaceholder(Rect contentRect)
	{
		var list = new Listing_Standard { maxOneColumn = true, ColumnWidth = contentRect.width - 20f };
		list.Begin(contentRect);
		list.Gap(8f);
		GUI.color = Color.gray;
		list.Label("EM.Nectar.PlaceholderTitle".Translate());
		list.Gap(6f);
		GUI.color = Color.white;
		list.Label("EM.Nectar.PlaceholderDesc".Translate());
		list.End();
	}

	private List<TabRecord> GetSubTabs()
	{
		switch (mainTabIndex)
		{
			case (int)MainTabIndex.Lactation:
				return new List<TabRecord>
				{
					new("EM.SubTab.BreastfeedSystem".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.BalanceCore".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.MastitisSystem".Translate(), () => subTabIndex = 2, subTabIndex == 2),
					new("EM.SubTab.OverflowPollution".Translate(), () => subTabIndex = 3, subTabIndex == 3),
					new("EM.SubTab.BalanceRealism".Translate(), () => subTabIndex = 4, subTabIndex == 4),
					new("EM.SubTab.BalanceAdvancedModel".Translate(), () => subTabIndex = 5, subTabIndex == 5),
					new("EM.SubTab.BalanceColonistExtras".Translate(), () => subTabIndex = 6, subTabIndex == 6)
				};
			case (int)MainTabIndex.Semen:
				return new List<TabRecord>
				{
					new("EM.SubTab.CumSystem".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.BallzIntegration".Translate(), () => subTabIndex = 1, subTabIndex == 1)
				};
			case (int)MainTabIndex.Permissions:
				return new List<TabRecord>
				{
					new("EM.SubTab.MenuVisibility".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.DefaultBehavior".Translate(), () => subTabIndex = 1, subTabIndex == 1)
				};
			case (int)MainTabIndex.Data:
				return new List<TabRecord>
				{
					new("EM.SubTab.Milk".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.MilkTags".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.RaceOverrides".Translate(), () => subTabIndex = 2, subTabIndex == 2),
					new("EM.SubTab.GenesAndAdvanced".Translate(), () => subTabIndex = 3, subTabIndex == 3)
				};
			case (int)MainTabIndex.Integrations:
				return new List<TabRecord>
				{
					new("EM.SubTab.RJWIntegration".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.NutritionSystem".Translate(), () => subTabIndex = 1, subTabIndex == 1)
				};
			case (int)MainTabIndex.Nectar:
			case (int)MainTabIndex.Debug:
			default:
				return new List<TabRecord>();
		}
	}
}
